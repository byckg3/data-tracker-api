using System.Text;
using DataTrackerApi.Infrastructure.Channels;
using DataTrackerApi.Models;

namespace DataTrackerApi.Services;

public class FileWriteWorker : BackgroundService
{
    private readonly DataChannel<ClientMessage> _channel;
    private readonly ILogger<FileWriteWorker> _logger;

    public FileWriteWorker( DataChannel<ClientMessage> channel, ILogger<FileWriteWorker> logger )
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
                    var payload = Encoding.UTF8.GetString( clientMessage.Payload.Span );

                    using ( Serilog.Context.LogContext.PushProperty( "ConnId", clientMessage.Id ) )
                    {
                        _logger.LogInformation( "{Payload}", payload );
                    }
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
        _logger.LogInformation( "FileWriteWorker stopped." );
    }
}