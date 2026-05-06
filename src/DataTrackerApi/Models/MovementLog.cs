namespace DataTrackerApi.Models;
public readonly record struct MovementLog : IEquatable<MovementLog>
{
    public MovementLog() { }
    public float[] Position { get; init; } = [];
    public float[] Rotation { get; init; } = [];

    public bool Equals( MovementLog other )
    {
        if ( Position.Length != other.Position.Length ||
             Rotation.Length != other.Rotation.Length )
            return false;

        return Position.AsSpan().SequenceEqual( other.Position.AsSpan() ) &&
               Rotation.AsSpan().SequenceEqual( other.Rotation.AsSpan() );
    }

    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach ( var v in Position ) hash.Add( v );

        foreach ( var v in Rotation ) hash.Add( v );

        return hash.ToHashCode();
    }
}