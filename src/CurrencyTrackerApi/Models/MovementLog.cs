namespace CurrencyTrackerApi.Models;
public readonly record struct MovementLog : IEquatable<MovementLog>
{
    public float[] Position { get; init; }
    public float[] Rotation { get; init; }

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
        return HashCode.Combine( Position, Rotation );
    }
}