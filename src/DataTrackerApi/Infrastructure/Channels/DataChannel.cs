using System.Threading.Channels;

namespace DataTrackerApi.Infrastructure.Channels;

public class DataChannel<T>
{
    private readonly Channel<T> _channel;

    public ChannelWriter<T> Writer { get; }
    public ChannelReader<T> Reader { get; }

    public DataChannel( int capacity = 1000 )
    {
        _channel = Channel.CreateBounded<T>(
            new BoundedChannelOptions( capacity )
            {
                SingleReader = true,  // singgle reader for file writing worker
                SingleWriter = false, // multiple writers from WebSocket connections
                FullMode = BoundedChannelFullMode.Wait
            }
        );

        Writer = _channel.Writer;
        Reader = _channel.Reader;
    }
}