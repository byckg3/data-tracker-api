
using System.IO.Pipelines;

namespace DataTrackerApi.Infrastructure.Tcp;
public class TcpMessageReceiver : BackgroundService
{
    private readonly ILogger<TcpMessageReceiver> _logger;
    private readonly PipelineManager _pipelineManager;
    private readonly string _host = "127.0.0.1";
    private readonly int _port = 8080;


    public TcpMessageReceiver(PipelineManager pipelineManager, ILogger<TcpMessageReceiver> logger)
    {
        _logger = logger;
        _pipelineManager = pipelineManager;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var writer = _pipelineManager.GetWriter();
            var client = new TcpClientService(writer, _logger);

            try
            {
                await client.ConnectAsync(_host, _port, stoppingToken);
                await client.ReceiveLoopAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while receiving message.");
            }
        }
    }
}