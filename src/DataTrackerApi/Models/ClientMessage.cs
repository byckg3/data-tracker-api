using System.Buffers;

namespace DataTrackerApi.Models;

public readonly struct ClientMessage : IDisposable
{
    public required string Id { get; init; }
    public Memory<byte> Payload { get; init; } = Memory<byte>.Empty;
    public bool IsConnected { get; init; }  = false;
    private readonly IMemoryOwner<byte>? _owner = null;

    public ClientMessage( IMemoryOwner<byte>? owner = null )
    {
        _owner = owner;
     }

    public ClientMessage() { }

    public void Dispose()
    {
        _owner?.Dispose();
    }
}