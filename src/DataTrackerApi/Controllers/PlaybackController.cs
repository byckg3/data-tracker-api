using System.Text.Json;
using DataTrackerApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace DataTrackerApi.Controllers;

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
    public IActionResult GetRecords( string connectionId, string date, CancellationToken ct )
    {
        try
        {
            var targetFilePath = Path.Combine( "logs", connectionId, $"status-{date}.log" );
            if ( !System.IO.File.Exists( targetFilePath ) )
            {
                throw new FileNotFoundException( "File not found", targetFilePath );
            }
            var playbackStream = _playbackService.StreamLogAsync( targetFilePath, ct );

            return Ok( playbackStream );
        }
        catch ( FileNotFoundException e )
        {
            _logger.LogError( e, "File not found: {FilePath}", e.FileName );
            return NotFound( "Data not found" );
        }
        catch ( Exception ex )
        {
            _logger.LogError( ex, "Error in PlaybackController.GetRecords" );
            return StatusCode( 500, "Internal server error" );
        }
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