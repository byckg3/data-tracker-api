using System.Net.WebSockets;
using System.Collections.Concurrent;
using System.Threading.Channels;
using System.Text;
using System.Buffers;
using DataTrackerApi.Infrastructure.Channels;
using DataTrackerApi.Models;

namespace DataTrackerApi.Services;
public class WebSocketService : IAsyncDisposable
{
    private readonly ConcurrentDictionary<string, ClientConnection> _connections = [];
    private readonly int BufferSize = 1024 * 4;
    private readonly DataDispatcher<ClientMessage> _channel;
    private readonly ILogger<WebSocketService> _logger;

    public WebSocketService( DataDispatcher<ClientMessage> channel, ILogger<WebSocketService> logger )
    {
        _channel = channel;
        _logger = logger;
    }

    public async Task ServeAsync( ClientConnection clientConnection, CancellationToken ct = default )
    {
        Task? forwardTask = null;
        try
        {
            bool added = AddConnection( clientConnection );
            if ( !added )
            {
                await CloseConnectionAsync( clientConnection );
                return;
            }

            forwardTask = Task.Run( async () =>
            {
                await foreach ( var msg in clientConnection.Reader.ReadAllAsync( ct ) )
                {
                    await _channel.SendAsync( msg, ct );
                }
            }, ct );

            await NotifyStatusChanged( clientConnection, true );
            await ListenAsync( clientConnection, ct );
        }
        catch ( Exception ex )
        {
            _logger.LogError( ex, "Unexpected error in WebSocketService.ServeAsync for {ConnectionId}", clientConnection.Id );
        }
        finally
        {
            await NotifyStatusChanged( clientConnection, false );
            RemoveConnection( clientConnection.Id );
            await CloseConnectionAsync( clientConnection );
            if ( forwardTask is not null )
            {
                try
                {
                    await forwardTask;
                }
                catch ( Exception ex ) when ( ex is OperationCanceledException or ChannelClosedException )
                {
                    _logger.LogDebug(
                        "Forward task stopped for connection {ConnectionId}: {ExceptionType}",
                        clientConnection.Id,
                        ex.GetType().Name
                    );
                }
            }
        }
    }

    public async Task SendMessageAsync( string connectionId, Memory<byte> message, CancellationToken ct = default )
    {
        if ( _connections.TryGetValue( connectionId, out var clientConnection ) &&
             clientConnection.Socket.State == WebSocketState.Open )
        {
            await clientConnection.Socket.SendAsync(
                message, WebSocketMessageType.Text, true, ct );
        }
        else
        {
            _logger.LogWarning( "Failed to send message. WebSocket with ID: {ConnectionId} is not open.", connectionId );
        }
    }

    private async Task ListenAsync( ClientConnection connection, CancellationToken ct = default )
    {
        var webSocket = connection.Socket;
        while ( webSocket.State == WebSocketState.Open )
        {
            var owner = MemoryPool<byte>.Shared.Rent( BufferSize );
            bool ownershipTransferred = false;
            try
            {
                var result = await webSocket.ReceiveAsync( owner.Memory, ct );
                if ( result.MessageType == WebSocketMessageType.Close )
                {
                    _logger.LogInformation( "Received close message from client {ConnectionId}.", connection.Id );
                    break;
                }

                var data = owner.Memory[ ..result.Count ];
                if ( _logger.IsEnabled( LogLevel.Debug ) )
                {
                    _logger.LogDebug(
                        "{ConnectionId}: {Message}", connection.Id, Encoding.UTF8.GetString( data.Span ) );
                    // await SendMessageAsync( connectionId, data, ct );
                }

                var clientMessage = new ClientMessage( owner )
                {
                    Id = connection.Id,
                    Payload = data,
                    IsConnected = true
                };
                await connection.Writer.WriteAsync( clientMessage, ct );
                ownershipTransferred = true;
            }
            catch ( Exception ex ) when ( ex is OperationCanceledException or WebSocketException )
            {
                // OperationCanceledException: Normal termination via server shutdown or CancellationToken.
                // WebSocketException: Network interruption, connection reset, or other socket-level errors; cannot be recovered.
                if ( ex is WebSocketException wsEx )
                    _logger.LogError(
                        wsEx,
                        "WebSocket error for connection {ConnectionId}: {ErrorCode}",
                        connection.Id, wsEx.WebSocketErrorCode
                    );
                break;
            }
            catch ( Exception ex )
            {
                // Unexpected error during message processing (e.g., channel write failure)
                // The socket itself may still be usable, log and continue to the next iteration
                _logger.LogError( ex, "Unexpected error processing message for connection ID: {ConnectionId}", connection.Id );
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
    private async Task NotifyStatusChanged( ClientConnection clientConnection, bool isOnline )
    {
        var message = new ClientMessage()
        {
            Id = clientConnection.Id,
            Payload = Encoding.UTF8.GetBytes( isOnline ? "[Connected]" : "[Disconnected]" ),
            IsConnected = isOnline
        };
        await clientConnection.Writer.WriteAsync( message, CancellationToken.None );
        _logger.LogInformation(
            "{ConnectionId} is now {Status}.", clientConnection.Id, isOnline ? "online" : "offline" );
    }

    private bool AddConnection( ClientConnection connection )
    {
        bool added = _connections.TryAdd( connection.Id, connection );
        if ( added )
        {
            // log the successful addition of the connection
            _logger.LogInformation( "ClientConnection added with ID: {ConnectionId}", connection.Id );
        }
        else
        {
            // Handle the case where the connection could not be added
            _logger.LogError( "Failed to add ClientConnection with ID: {ConnectionId}", connection.Id );
        }
        return added;
    }

    private bool RemoveConnection( string id )
    {
        bool removed = _connections.TryRemove( id, out var _ );
        if ( removed )
        {
            _logger.LogInformation( "ClientConnection removed with ID: {ConnectionId}", id );
        }
        else
        {
            // Handle the case where the connection could not be removed
            _logger.LogWarning( "Failed to remove ClientConnection with ID: {ConnectionId}", id );
        }
        return removed;
    }

    private async Task CloseConnectionAsync( ClientConnection connection )
    {
        try
        {
            await connection.DisposeAsync();
            _logger.LogInformation(
                "Closed WebSocket connection with ID: {ConnectionId}", connection.Id );
        }
        catch ( Exception ex )
        {
            _logger.LogError(
                ex, "Error while closing WebSocket connection with ID: {ConnectionId}", connection.Id );
        }
    }

    public async ValueTask DisposeAsync()
    {
        foreach ( var connection in _connections.Values )
        {
            await CloseConnectionAsync( connection );
        }
        _logger.LogInformation( "WebSocketService disposed and all connections closed." );
    }
}