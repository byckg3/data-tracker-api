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

    public PlaybackController( PlaybackService playbackService )
    {
        _playbackService = playbackService;
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

                await Response.WriteAsync( $"data: {jsonString}\n\n", ct );
                await Response.Body.FlushAsync( ct );
                await Task.Delay( 1000, ct );
            }
        }
        catch ( OperationCanceledException )
        {
            Console.WriteLine( "Playback cancelled by client." );
        }
        catch ( Exception ex )
        {
            Console.WriteLine( $"Error in PlaybackController.Play:\n{ex.Message}" );
            try
            {
                await Response.WriteAsync( $"data: Error: {ex.Message}\n\n", ct );
                await Response.Body.FlushAsync( ct );
            }
            catch ( Exception innerEx )
            {
                Console.WriteLine( $"Error sending error message to client:\n{innerEx.Message}" );
            }
        }
    }
}