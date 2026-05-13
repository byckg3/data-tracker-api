namespace DataTrackerApi.Services.Workers;

public class ClientFileFlushScheduler : BackgroundService
{
    private readonly ClientFileManager _clientFileManager;
    private readonly ILogger<ClientFileFlushScheduler> _logger;
    private readonly TimeSpan _flushInterval = TimeSpan.FromSeconds( 3 );

    public ClientFileFlushScheduler( ClientFileManager clientFileManager, ILogger<ClientFileFlushScheduler> logger )
    {
        _clientFileManager = clientFileManager;
        _logger = logger;
    }

    protected override async Task ExecuteAsync( CancellationToken stoppingToken )
    {
        try
        {
            _logger.LogInformation( "FileFlushScheduler started." );
            using var timer = new PeriodicTimer( _flushInterval );

            while ( await timer.WaitForNextTickAsync( stoppingToken ) )
            {
                try
                {
                    await _clientFileManager.FlushAllAsync( stoppingToken );
                }
                catch ( OperationCanceledException )
                {
                    _logger.LogInformation( "ClientFileFlushScheduler is stopping due to cancellation." );
                    break;
                }
                catch ( Exception ex )
                {
                    _logger.LogError( ex, "Error flushing file stream for writer." );
                }
            }
        }
        catch ( OperationCanceledException )
        {
            _logger.LogInformation( "ClientFileFlushScheduler is stopping due to cancellation." );
        }
        catch ( Exception ex )
        {
            _logger.LogError( ex, "An error occurred in the ClientFileFlushScheduler." );
        }
        finally
        {
            await _clientFileManager.FlushAllAsync();
            _logger.LogInformation( "ClientFileFlushScheduler stopped." );
        }
    }
}