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

    public void AddSocket( string id, WebSocket socket )
    {
        if ( _sockets.TryAdd( id, socket ) )
        {
            // log the successful addition of the socket
            Console.WriteLine( $"WebSocket added with ID: {id}" );
        }
        else
        {
            // Handle the case where the socket could not be added
            Console.WriteLine( $"Failed to add WebSocket with ID: {id}" );
        }
    }

    public async Task RemoveSocket( string id )
    {
        if ( _sockets.TryRemove( id, out var socket) )
        {
            // ensure the socket is closed after removing it
            if ( socket.State == WebSocketState.Open )
                await CloseSocket( socket );

            Console.WriteLine( $"WebSocket removed with ID: {id}" );
        }
        else
        {
            // Handle the case where the socket could not be removed
            Console.WriteLine( $"Failed to remove WebSocket with ID: {id}" );
        }
    }

    public async Task ListenAsync( string connectionId, WebSocket webSocket, CancellationToken ct = default )
    {
        try
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
                        await CloseSocket( webSocket );
                        break;
                    }
                    var data = owner.Memory[ ..result.Count ];
                    await SendMessage( connectionId, data, ct );

                    var clientMessage = new ClientMessage( connectionId, data, owner );
                    await _channel.Writer.WriteAsync( clientMessage, ct );
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
        catch ( Exception ex )
        {
            Console.WriteLine( $"WebSocket error:\n{ex.Message}" );
        }
        finally
        {
            webSocket.Abort();
        }
    }

    public async Task SendMessage( string connectionId, Memory<byte> message, CancellationToken ct = default )
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

    public void Output( byte[] buffer, int count )
    {
        using var stdout = Console.OpenStandardOutput();
        stdout.Write( buffer, 0, count );
        stdout.Write( Encoding.UTF8.GetBytes( Environment.NewLine ) );
    }

    private async Task CloseSocket( WebSocket socket )
    {
        using var closeCts = new CancellationTokenSource( TimeSpan.FromSeconds( 3 ) );
        await socket.CloseAsync(
            socket.CloseStatus ?? WebSocketCloseStatus.NormalClosure,
            socket.CloseStatusDescription ?? "Closed by server",
            closeCts.Token );
    }
}