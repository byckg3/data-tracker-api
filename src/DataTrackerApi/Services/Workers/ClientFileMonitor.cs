namespace DataTrackerApi.Services.Workers;

public class ClientFileMonitor : BackgroundService
{
    private readonly ClientFileManager _fileManager;
    private readonly ILogger<ClientFileMonitor> _logger;
    private readonly TimeSpan _inactivityThreshold = TimeSpan.FromMinutes( 5 );

    public ClientFileMonitor( ClientFileManager fileManager, ILogger<ClientFileMonitor> logger )
    {
        _fileManager = fileManager;
        _logger = logger;
    }

    protected override async Task ExecuteAsync( CancellationToken stoppingToken )
    {
        try
        {
            _logger.LogInformation( "ClientFileMonitor started." );
            using var timer = new PeriodicTimer( TimeSpan.FromMinutes( 3 ) );

            while ( await timer.WaitForNextTickAsync( stoppingToken ) )
            {
                try
                {
                    int removed = await _fileManager.CleanExpiredAsync( _inactivityThreshold, stoppingToken );
                    if ( removed > 0 )
                    {
                        _logger.LogInformation( "Closed {Count} inactive file(s).", removed );
                    }
                }
                catch ( Exception ex ) when ( ex is not OperationCanceledException )
                {
                    _logger.LogError( ex, "Error monitoring client files." );
                }
            }
        }
        catch ( OperationCanceledException )
        {
            _logger.LogInformation( "ClientFileMonitor is stopping due to cancellation." );
        }
        catch ( Exception ex )
        {
            _logger.LogError( ex, "An error occurred in the ClientFileMonitor." );
        }
        finally
        {
            _logger.LogInformation( "ClientFileMonitor stopped." );
        }
    }
}