using System.Net.WebSockets;
using System.Collections.Concurrent;
using System.Text;
using System.Buffers;
using DataTrackerApi.Infrastructure.Channels;
using DataTrackerApi.Models;

namespace DataTrackerApi.Services;
public class WebSocketService
{
    private static readonly ConcurrentDictionary<string, WebSocket> _sockets = [];
    private static readonly int BufferSize = 1024 * 4;
    private readonly DataChannel<ClientMessage> _channel;
    private readonly ILogger<WebSocketService> _logger;

    public WebSocketService( DataChannel<ClientMessage> channel, ILogger<WebSocketService> logger )
    {
        _channel = channel;
        _logger = logger;
    }

    public async Task ServeAsync( string connectionId, WebSocket webSocket, CancellationToken ct = default )
    {
        try
        {
            bool added = AddSocket( connectionId, webSocket );
            if ( !added )
            {
                await CloseSocketAsync( webSocket );
                return;
            }
            await NotifyStatusChanged( connectionId, true );
            await ListenAsync( connectionId, webSocket, ct );
        }
        catch ( Exception ex )
        {
            _logger.LogError( ex, "Unexpected error in WebSocketService.ServeAsync for {ConnectionId}", connectionId );
        }
        finally
        {
            bool removed = RemoveSocket( connectionId );
            if ( removed )
            {
                await NotifyStatusChanged( connectionId, false );
            }
            await CloseSocketAsync( webSocket );
        }
    }

    public async Task SendMessageAsync( string connectionId, Memory<byte> message, CancellationToken ct = default )
    {
        if ( _sockets.TryGetValue( connectionId, out var socket ) && socket.State == WebSocketState.Open )
        {
            await socket.SendAsync(
                message,
                WebSocketMessageType.Text,
                true,
                ct );
        }
        else
        {
            _logger.LogWarning( "Failed to send message. WebSocket with ID: {ConnectionId} is not open.", connectionId );
        }
    }

    private async Task ListenAsync( string connectionId, WebSocket webSocket, CancellationToken ct = default )
    {
        while ( webSocket.State == WebSocketState.Open )
        {
            var owner = MemoryPool<byte>.Shared.Rent( BufferSize );
            bool ownershipTransferred = false;
            try
            {
                var result = await webSocket.ReceiveAsync( owner.Memory, ct );
                if ( result.MessageType == WebSocketMessageType.Close )
                {
                    _logger.LogInformation( "Received close message from client {ConnectionId}.", connectionId );
                    await CloseSocketAsync( webSocket );

                    break;
                }
                var data = owner.Memory[ ..result.Count ];
                if ( _logger.IsEnabled( LogLevel.Debug ) )
                {
                    _logger.LogDebug( "{Message}", Encoding.UTF8.GetString( data.Span ) );
                    // await SendMessageAsync( connectionId, data, ct );
                }

                var clientMessage = new ClientMessage( owner )
                {
                    Id = connectionId,
                    Payload = data,
                    IsConnected = true
                };
                await _channel.Writer.WriteAsync( clientMessage, ct );
                ownershipTransferred = true;
            }
            catch ( Exception ex ) when ( ex is OperationCanceledException or WebSocketException )
            {
                // OperationCanceledException: Normal termination via server shutdown or CancellationToken.
                // WebSocketException: Network interruption, connection reset, or other socket-level errors; cannot be recovered.
                if ( ex is WebSocketException wsEx )
                    _logger.LogWarning(
                        wsEx,
                        "WebSocket error for connection {ConnectionId}: {ErrorCode}",
                        connectionId, wsEx.WebSocketErrorCode
                    );
                break;
            }
            catch ( Exception ex )
            {
                // Unexpected error during message processing (e.g., channel write failure)
                // The socket itself may still be usable, log and continue to the next iteration
                _logger.LogError( ex, "Unexpected error processing message for connection ID: {ConnectionId}", connectionId );
            }
            finally
            {
                if ( !ownershipTransferred )
                {
                    owner.Dispose();
                }
            }
        }
    }

    // TODO: multiple status
    private async Task NotifyStatusChanged( string connectionId, bool isOnline )
    {
        var message = new ClientMessage()
        {
            Id = connectionId,
            Payload = Encoding.UTF8.GetBytes( isOnline ? "[Connected]" : "[Disconnected]" ),
            IsConnected = isOnline
        };
        await _channel.Writer.WriteAsync( message, CancellationToken.None );
        _logger.LogInformation( "{ConnectionId} is now {Status}.", connectionId, isOnline ? "online" : "offline" );
    }

    private bool AddSocket( string id, WebSocket socket )
    {
        bool added = _sockets.TryAdd( id, socket );
        if ( added )
        {
            // log the successful addition of the socket
            _logger.LogInformation( "WebSocket added with ID: {ConnectionId}", id );
        }
        else
        {
            // Handle the case where the socket could not be added
            _logger.LogError( "Failed to add WebSocket with ID: {ConnectionId}", id );
        }
        return added;
    }

    private bool RemoveSocket( string id )
    {
        bool removed = _sockets.TryRemove( id, out var _ );
        if ( removed )
        {
            _logger.LogInformation( "WebSocket removed with ID: {ConnectionId}", id );
        }
        else
        {
            // Handle the case where the socket could not be removed
            _logger.LogWarning( "Failed to remove WebSocket with ID: {ConnectionId}", id );
        }
        return removed;
    }

    private async Task CloseSocketAsync( WebSocket socket )
    {
        if ( socket.State is WebSocketState.Open or WebSocketState.CloseReceived )
        {
            try
            {
                using var closeCts = new CancellationTokenSource( TimeSpan.FromSeconds( 3 ) );
                await socket.CloseAsync(
                    socket.CloseStatus ?? WebSocketCloseStatus.NormalClosure,
                    socket.CloseStatusDescription ?? "Closed by server",
                    closeCts.Token );
            }
            catch ( Exception ex )
            {
                _logger.LogError( ex, "Error closing WebSocket" );
            }
        }
    }
}