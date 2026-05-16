using DataTrackerApi.Infrastructure.Channels;
using DataTrackerApi.Models;

namespace DataTrackerApi.Services.Workers;

public class ClientMessageConsumer : BackgroundService
{
    private readonly DataDispatcher<ClientMessage> _channel;
    private readonly ClientFileManager _clientFileManager;
    private readonly ILogger<ClientMessageConsumer> _logger;

    public ClientMessageConsumer( DataDispatcher<ClientMessage> channel,
                                  ClientFileManager clientFileManager,
                                  ILogger<ClientMessageConsumer> logger )
    {
        _channel = channel;
        _clientFileManager = clientFileManager;
        _logger = logger;
    }

    protected override async Task ExecuteAsync( CancellationToken stoppingToken )
    {
        try
        {
            _logger.LogInformation( "FileWriteWorker started." );
            await foreach ( var clientMessage in _channel.ReceiveAllAsync( stoppingToken ) )
            {
                try
                {
                    using var _ = clientMessage;
                    await _clientFileManager.WriteToClientFileAsync( clientMessage, stoppingToken );
                }
                catch ( Exception ex )
                {
                    _logger.LogError( ex, "Error processing client message with ID: {Id}", clientMessage.Id );
                }
            }
        }
        catch ( OperationCanceledException )
        {
            _logger.LogInformation( "ClientMessageConsumer is stopping due to cancellation." );
        }
        catch ( Exception ex )
        {
            _logger.LogError( ex, "An error occurred in the ClientMessageConsumer." );
        }
        finally
        {
            await _clientFileManager.FlushAllAsync();
            _logger.LogInformation( "ClientMessageConsumer stopped." );
        }
    }
}