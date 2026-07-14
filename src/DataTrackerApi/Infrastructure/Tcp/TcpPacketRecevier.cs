
using System.IO.Pipelines;

namespace DataTrackerApi.Infrastructure.Tcp;
public class TcpPacketReceiver : BackgroundService
{
    private readonly ILogger<TcpPacketReceiver> _logger;
    private readonly PipelineManager _pipelineManager;
    private readonly string _host = "127.0.0.1";
    private readonly int _port = 8080;


    public TcpPacketReceiver(PipelineManager pipelineManager, ILogger<TcpPacketReceiver> logger)
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