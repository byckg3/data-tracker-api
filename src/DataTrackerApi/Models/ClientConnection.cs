using System.Net.WebSockets;
using System.Threading.Channels;
using DataTrackerApi.Models;

public class ClientConnection : IAsyncDisposable
{
    public string Id { get; init; }
    public WebSocket Socket { get; init; }
    public ChannelWriter<ClientMessage> Writer { get; init; }
    public ChannelReader<ClientMessage> Reader { get; init; }
    private readonly Channel<ClientMessage> _messageChannel;

    public ClientConnection( string id, WebSocket socket )
    {
        Id = id;
        Socket = socket;
        _messageChannel = Channel.CreateBounded<ClientMessage>( 500 );
        Writer = _messageChannel.Writer;
        Reader = _messageChannel.Reader;
    }

    public async ValueTask DisposeAsync()
    {
        _messageChannel.Writer.TryComplete();
        if ( Socket.State is WebSocketState.Open or WebSocketState.CloseReceived )
        {
            using var closeCts = new CancellationTokenSource( TimeSpan.FromSeconds( 3 ) );
            await Socket.CloseAsync(
                Socket.CloseStatus ?? WebSocketCloseStatus.NormalClosure,
                Socket.CloseStatusDescription ?? "Closed by server",
                closeCts.Token );
        }
    }
}