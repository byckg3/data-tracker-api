namespace CurrencyTrackerApi.Models;
public readonly record struct MovementLog
{
    public float[] Position { get; init; }
    public float[] Rotation { get; init; }
}