using System.Net.WebSockets;
using Microsoft.AspNetCore.Mvc;
using CurrencyTrackerApi.Services;

namespace CurrencyTrackerApi.Controllers;

public class WebSocketController : ControllerBase
{
    private readonly WebSocketService _webSocketService;

    public WebSocketController( WebSocketService webSocketService )
    {
        _webSocketService = webSocketService;
    }

    [Route("/ws")]
    public async Task Get()
    {
        if ( HttpContext.WebSockets.IsWebSocketRequest )
        {
            using var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
            string connectionId = Guid.NewGuid().ToString();

            await _webSocketService.ServeAsync( connectionId, webSocket, HttpContext.RequestAborted );
        }
        else
        {
            HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
        }
    }
}