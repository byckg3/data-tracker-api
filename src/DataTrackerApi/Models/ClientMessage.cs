using System.Buffers;

namespace DataTrackerApi.Models;

public readonly struct ClientMessage : IDisposable
{
    public string Id { get; init; }
    public Memory<byte> Payload { get; init; }
    private readonly IMemoryOwner<byte> _owner;

    public ClientMessage( string id, Memory<byte> payload, IMemoryOwner<byte> owner )
    {
        _owner = owner;
        Id = id;
        Payload = payload;
    }

    public void Dispose()
    {
        _owner?.Dispose();
    }
}