using System.Collections.Concurrent;
using System.Text;
using DataTrackerApi.Infrastructure.Channels;
using DataTrackerApi.Infrastructure.Settings;
using DataTrackerApi.Models;

namespace DataTrackerApi.Services;

public class ClientFileWorker : BackgroundService
{
    private readonly ConcurrentDictionary<string, FileStream> _writers = [];
    private readonly DataChannel<ClientMessage> _channel;
    private readonly ILogger<ClientFileWorker> _logger;
    public string BaseDir { get; set; } = Path.Combine( FileSettings.BaseDirectory, "logs" );

    public ClientFileWorker( DataChannel<ClientMessage> channel, ILogger<ClientFileWorker> logger )
    {
        _channel = channel;
        _logger = logger;
    }

    protected override async Task ExecuteAsync( CancellationToken stoppingToken )
    {
        try
        {
            _logger.LogInformation( "FileWriteWorker started." );
            await foreach ( var clientMessage in _channel.Reader.ReadAllAsync( stoppingToken ) )
            {
                try
                {
                    using var _ = clientMessage;
                    if ( !clientMessage.IsConnected && _writers.ContainsKey( clientMessage.Id ) )
                    {
                        await CloseAsync( clientMessage.Id );
                        _logger.LogInformation( "Client {Id} has left.", clientMessage.Id );

                        continue;
                    }

                    if ( !_writers.ContainsKey( clientMessage.Id ) )
                    {
                        var stream = CreateFileStream( clientMessage.Id );
                        if ( !_writers.TryAdd( clientMessage.Id, stream ) )
                        {
                            // Handle the case where the writer could not be added
                            await stream.DisposeAsync();
                        }
                    }
                    await WriteLineAsync( clientMessage.Id, clientMessage.Payload, stoppingToken );
                }
                catch ( Exception ex )
                {
                    _logger.LogError( ex, "Error processing client message with ID: {Id}", clientMessage.Id );
                }
            }
        }
        catch ( OperationCanceledException )
        {
            _logger.LogInformation( "FileWriteWorker is stopping due to cancellation." );
        }
        catch ( Exception ex )
        {
            _logger.LogError( ex, "An error occurred in the FileWriteWorker." );
        }
        finally
        {
            await CloseAllAsync();
            _logger.LogInformation( "FileWriteWorker stopped." );
        }
    }

    private FileStream CreateFileStream( string connectionId )
    {
        string clientDir = connectionId;
        string folderPath = Path.Combine( BaseDir, clientDir );
        Directory.CreateDirectory( folderPath );

        string fileName = DateTime.UtcNow.ToString( "yyyyMMdd_HHmmss" );
        string fullPath = Path.Combine( folderPath, $"{fileName}.txt" );

        return new FileStream(
            fullPath, FileMode.Append, FileAccess.Write, FileShare.Read, 4096, true );
    }

    private async Task WriteLineAsync( string connectionId, Memory<byte> data, CancellationToken cancellationToken = default )
    {
        if ( _writers.TryGetValue( connectionId, out var writer ) )
        {
            await writer.WriteAsync( data , cancellationToken );
            await writer.WriteAsync(
                Encoding.UTF8.GetBytes( Environment.NewLine ) , cancellationToken );

            await writer.FlushAsync( cancellationToken );
        }
    }

    private async Task CloseAsync( string connectionId )
    {
        if ( _writers.TryRemove( connectionId, out var writer ) )
        {
            await writer.DisposeAsync();
        }
    }

    private async Task CloseAllAsync()
    {
        foreach ( var ( id, writer ) in _writers )
        {
            await writer.DisposeAsync();
        }
        _writers.Clear();
    }

    private void LogMessage( string connectionId, Memory<byte> data )
    {
        string payload = Encoding.UTF8.GetString( data.Span );
        using ( Serilog.Context.LogContext.PushProperty( "ConnId", connectionId ) )
        {
            _logger.LogInformation( "{Payload}", payload );
        }
    }
}