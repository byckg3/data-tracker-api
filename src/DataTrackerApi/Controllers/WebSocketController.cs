using System.Net.WebSockets;
using Microsoft.AspNetCore.Mvc;
using DataTrackerApi.Services;

namespace DataTrackerApi.Controllers;

//
public class WebSocketController : ControllerBase
{
    private readonly WebSocketService _webSocketService;
    private readonly ILogger<WebSocketController> _logger;

    public WebSocketController( WebSocketService webSocketService, ILogger<WebSocketController> logger )
    {
        _webSocketService = webSocketService;
        _logger = logger;
    }

    [Route("/")]
    public async Task Get( CancellationToken ct )
    {
        if ( HttpContext.WebSockets.IsWebSocketRequest )
        {
            using var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
            string connectionId = Guid.NewGuid().ToString();
            _logger.LogInformation( "New WebSocket connection established: {ConnectionId}", connectionId );

            await _webSocketService.ServeAsync( connectionId, webSocket, ct );
        }
        else
        {
            HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
        }
    }
}