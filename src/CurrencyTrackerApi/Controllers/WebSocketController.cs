using System.Net.WebSockets;
using Microsoft.AspNetCore.Mvc;
using CurrencyTrackerApi.Services;

namespace CurrencyTrackerApi.Controllers;

public class WebSocketController : ControllerBase
{
    private readonly WebSocketService _webSocketService;
    private readonly ILogger<WebSocketController> _logger;

    public WebSocketController( WebSocketService webSocketService, ILogger<WebSocketController> logger )
    {
        _webSocketService = webSocketService;
        _logger = logger;
    }

    [Route("/ws")]
    public async Task Get()
    {
        if ( HttpContext.WebSockets.IsWebSocketRequest )
        {
            using var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
            string connectionId = Guid.NewGuid().ToString();
            _logger.LogInformation( "New WebSocket connection established: {ConnectionId}", connectionId );

            await _webSocketService.ServeAsync( connectionId, webSocket, HttpContext.RequestAborted );
        }
        else
        {
            HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
        }
    }
}