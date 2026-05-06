using System.Runtime.CompilerServices;
using System.Text.Json;
using CurrencyTrackerApi.Infrastructure.Settings;
using CurrencyTrackerApi.Models;

namespace CurrencyTrackerApi.Services;

public class PlaybackService
{
    public static string BaseDir { get; set; } = FileSettings.BaseDirectory;

    public async IAsyncEnumerable<MovementLog> StreamLogAsync(
        string filePath, [EnumeratorCancellation] CancellationToken ct = default )
    {
        var fullPath = Path.Combine( BaseDir, filePath );
        using var fs = new FileStream( fullPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite );
        using var reader = new StreamReader( fs );

        while ( await reader.ReadLineAsync( ct ) is string line )
        {
            var trimmedLine = line.AsSpan().Trim();
            if ( trimmedLine.IsEmpty )
                continue;

            var log = JsonSerializer.Deserialize<MovementLog>( trimmedLine );
            // Console.WriteLine( $"Position: {string.Join( ", ", log.Position )}, Rotation: {string.Join( ", ", log.Rotation )}" );

            yield return log;
        }
    }
}