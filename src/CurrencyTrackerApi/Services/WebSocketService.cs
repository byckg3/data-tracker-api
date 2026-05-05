using System.Net.WebSockets;
using System.Collections.Concurrent;
using System.Text;
using System.Buffers;
using CurrencyTrackerApi.Infrastructure.Channels;
using CurrencyTrackerApi.Models;

namespace CurrencyTrackerApi.Services;
public class WebSocketService
{
    private static readonly ConcurrentDictionary<string, WebSocket> _sockets = [];
    private static readonly int BufferSize = 1024 * 4;
    private readonly DataChannel<ClientMessage> _channel;

    public WebSocketService( DataChannel<ClientMessage> channel )
    {
        _channel = channel;
    }

    public async Task ServeAsync( string connectionId, WebSocket webSocket, CancellationToken ct = default )
    {
        try
        {
            AddSocket( connectionId, webSocket );
            await ListenAsync( connectionId, webSocket, ct );
        }
        catch ( Exception ex )
        {
            Console.WriteLine( $"Error in WebSocketService.ServeAsync:\n{ex.Message}" );
        }
        finally
        {
            await RemoveSocketAsync( connectionId );
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
            Console.WriteLine( $"Failed to send message. WebSocket with ID: {connectionId} is not open." );
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
                    Console.WriteLine( "Received close message from client." );
                    await CloseSocketAsync( webSocket );
                    break;
                }
                var data = owner.Memory[ ..result.Count ];
                await SendMessageAsync( connectionId, data, ct );

                var clientMessage = new ClientMessage( connectionId, data, owner );
                await _channel.Writer.WriteAsync( clientMessage, ct );
            }
            catch ( Exception ex )
            {
                Console.WriteLine( $"Error in WebSocketService.ListenAsync for connection ID: {connectionId}\n{ex.Message}" );
            }
            finally
            {
                // Ensure that the rented memory is returned to the pool if ownership was not transferred
                if ( !ownershipTransferred )
                {
                    owner.Dispose();
                }
            }
        }
        Console.WriteLine( "WebSocket connection closed." );
    }

    private static bool AddSocket( string id, WebSocket socket )
    {
        bool added = _sockets.TryAdd( id, socket );
        if ( added )
        {
            // log the successful addition of the socket
            Console.WriteLine( $"WebSocket added with ID: {id}" );
        }
        else
        {
            // Handle the case where the socket could not be added
            Console.WriteLine( $"Failed to add WebSocket with ID: {id}" );
        }
        return added;
    }

    private static async Task RemoveSocketAsync( string id )
    {
        if ( _sockets.TryRemove( id, out var _ ) )
        {
            Console.WriteLine( $"WebSocket removed with ID: {id}" );
        }
        else
        {
            // Handle the case where the socket could not be removed
            Console.WriteLine( $"Failed to remove WebSocket with ID: {id}" );
        }
    }

    private static async Task CloseSocketAsync( WebSocket socket )
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
                Console.WriteLine( $"Error closing WebSocket: {ex.Message}" );
            }
        }
    }
}