using System.Collections.Concurrent;
using CurrencyTrackerApi.Hubs.Models;
using Microsoft.AspNetCore.SignalR;

namespace CurrencyTrackerApi.Hubs;

public class ChatHub : Hub
{
    private static readonly ConcurrentDictionary<string, string> _connections = [];

    public override async Task OnConnectedAsync()
    {
        string cid = Context.ConnectionId;
        var userName = cid;

        _connections[ userName ] = cid;
        Console.WriteLine( $"New connection: {cid}" );

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync( Exception? exception )
    {
        string cid = Context.ConnectionId;
        _connections.TryRemove( cid, out _ );

        if ( exception != null )
        {
            Console.WriteLine( $"Connection {cid} disconnected with error: {exception.Message}" );
        }
        else
        {
            Console.WriteLine( $"Connection {cid} disconnected." );
        }
        await base.OnDisconnectedAsync( exception );
    }

    public async Task SendMessage( string targetUser, string message )
    {
        Console.WriteLine( $"Send message to {targetUser}: {message}" );

        var targetId = _connections.GetValueOrDefault( targetUser );
        if ( targetId == null )
        {
            var messageToCaller = $"User '{targetUser}' not found.";
            await Clients.Caller.SendAsync( "ReceiveMessage", messageToCaller );
            // throw new HubException( messageToCaller );
            // return new HubResponse { Success = false, Message = messageToCaller };
            return;
        }

        var senderId = Context.ConnectionId;
        await Clients.Client( targetId ).SendAsync( "ReceiveMessage", senderId, message );
        // return new HubResponse { Success = true, Message = "Message sent successfully." };
    }

    public async Task BroadcastMessage( string message )
    {
        Console.WriteLine( $"Broadcast message: {message}" );
        await Clients.All.SendAsync( "ReceiveMessage", message );
        await Clients.Caller.SendAsync( "ReceiveMessage", "Broadcast complete" );
        // return new HubResponse { Success = true, Message = "Broadcast complete" };
    }
}