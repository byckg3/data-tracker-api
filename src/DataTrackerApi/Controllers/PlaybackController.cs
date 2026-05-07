using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

using DataTrackerApi.Services;
using Settings = DataTrackerApi.Infrastructure.Settings;
using DataTrackerApi.Models;

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
    public ActionResult<IAsyncEnumerable<MovementLog>> GetRecords( string connectionId, string date )
    {
        try
        {
            var ( isSuccess, error, stream ) = _playbackService.PrepareStream( connectionId, date );
            return ( isSuccess, error ) switch
            {
                ( true, _ )                    => Ok( stream ),
                ( false, "File not found" )    => NotFound( error ),
                ( false, _ )                   => BadRequest( error ),
            };
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
            var ( isSuccess, error, stream ) = _playbackService.PrepareStream( connectionId, date );
            if ( !isSuccess || stream is null )
            {
                await Response.WriteAsync( $"data: Error: {error}\n\n", ct );
                await Response.Body.FlushAsync( ct );
                return;
            }

            await foreach ( var record in stream.WithCancellation( ct ) )
            {
                string jsonString = JsonSerializer.Serialize( record, Settings.JsonOptions.Default );
                if ( _logger.IsEnabled( LogLevel.Debug ) )
                    _logger.LogDebug( "{Record}", jsonString );

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
                await Response.WriteAsync( "data: Error: Internal server error\n\n", ct );
                await Response.Body.FlushAsync( ct );
            }
            catch ( Exception innerEx )
            {
                _logger.LogError( innerEx, "Error sending error message to client" );
            }
        }
    }
}