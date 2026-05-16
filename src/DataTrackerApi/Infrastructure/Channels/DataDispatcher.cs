using System.Threading.Channels;

namespace DataTrackerApi.Infrastructure.Channels;

public class DataDispatcher<T> : IAsyncDisposable
{
    private readonly Channel<T> _channel;
    private readonly ILogger<DataDispatcher<T>> _logger;
    public int Count => _channel.Reader.Count;
    public bool IsEmpty => _channel.Reader.Count == 0;

    public DataDispatcher( IHostApplicationLifetime lifetime,
                           ILogger<DataDispatcher<T>> logger,
                           int capacity = 1000 )
    {
        var options = new BoundedChannelOptions( capacity )
        {
            SingleReader = true,  // singgle reader for file writing worker
            SingleWriter = false, // multiple writers from WebSocket connections
            FullMode = BoundedChannelFullMode.Wait
        };
        _channel = Channel.CreateBounded<T>( options );
        _logger = logger;

        lifetime.ApplicationStopping.Register( () =>
        {
            _channel.Writer.TryComplete();
            _logger.LogInformation(
                "Application is stopping. DataDispatcher<{Type}> channel is being completed.", typeof( T ).Name );
        } );
    }

    public async ValueTask SendAsync( T item, CancellationToken ct = default )
    {
        await _channel.Writer.WriteAsync( item, ct );
    }

    public IAsyncEnumerable<T> ReceiveAllAsync( CancellationToken ct = default )
    {
        return _channel.Reader.ReadAllAsync( ct );
    }

    public async ValueTask DisposeAsync()
    {
        _channel.Writer.TryComplete();
        GC.SuppressFinalize( this );

        _logger.LogInformation( "DataDispatcher<{Type}> disposed and channel completed.", typeof( T ).Name );
    }
}