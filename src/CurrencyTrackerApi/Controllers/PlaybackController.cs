using System.Runtime.CompilerServices;
using System.Text.Json;
using CurrencyTrackerApi.Models;
using CurrencyTrackerApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace CurrencyTrackerApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PlaybackController : ControllerBase
{
    private readonly PlaybackService _playbackService;
    private readonly ILogger<PlaybackController> _logger;

    public PlaybackController( PlaybackService playbackService, ILogger<PlaybackController> logger )
    {
        _playbackService = playbackService;
        _logger = logger;
    }

    [HttpGet( "{connectionId}/{date}" )]
    public IAsyncEnumerable<MovementLog> GetRecords( string connectionId, string date, CancellationToken ct )
    {
        var targetFilePath = Path.Combine( "logs", connectionId, $"status-{date}.log" );
        var playbackStream = _playbackService.StreamLogAsync( targetFilePath, ct );

        return playbackStream;
    }

    [HttpGet( "sse/{connectionId}/{date}" )]
    public async Task Play( string connectionId, string date, CancellationToken ct )
    {
        Response.Headers.ContentType = "text/event-stream";
        Response.Headers.CacheControl = "no-cache";
        Response.Headers.Connection = "keep-alive";
        try
        {
            var targetFilePath = Path.Combine( "logs", connectionId, $"status-{date}.log" );
            var playbackStream = _playbackService.StreamLogAsync( targetFilePath, ct );

            await foreach ( var record in playbackStream.WithCancellation( ct ) )
            {
                if ( ct.IsCancellationRequested )
                {
                    break;
                }
                string jsonString = JsonSerializer.Serialize( record );
                _logger.LogInformation( "{Record}", jsonString );

                await Response.WriteAsync( $"data: {jsonString}\n\n", ct );
                await Response.Body.FlushAsync( ct );
                await Task.Delay( 1000, ct );
            }
        }
        catch ( OperationCanceledException )
        {
            _logger.LogInformation( "Playback cancelled by client." );
        }
        catch ( Exception ex )
        {
            _logger.LogError( ex, "Error in PlaybackController.Play" );
            try
            {
                await Response.WriteAsync( $"data: Error: {ex.Message}\n\n", ct );
                await Response.Body.FlushAsync( ct );
            }
            catch ( Exception innerEx )
            {
                _logger.LogError( innerEx, "Error sending error message to client" );
            }
        }
    }
}