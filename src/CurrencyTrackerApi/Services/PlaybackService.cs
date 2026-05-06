using System.Runtime.CompilerServices;
using System.Text.Json;
using CurrencyTrackerApi.Infrastructure.Settings;
using CurrencyTrackerApi.Models;

namespace CurrencyTrackerApi.Services;

public class PlaybackService
{
    public static string BaseDir { get; set; } = FileSettings.BaseDirectory;
    private readonly ILogger<PlaybackService> _logger;

    public PlaybackService( ILogger<PlaybackService> logger )
    {
        _logger = logger;
    }

    public async IAsyncEnumerable<MovementLog> StreamLogAsync(
        string filePath, [EnumeratorCancellation] CancellationToken ct = default )
    {
        var fullPath = Path.Combine( BaseDir, filePath );
        using var fs = new FileStream( fullPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite );
        using var reader = new StreamReader( fs );

        MovementLog? lastLog = null;
        while ( await reader.ReadLineAsync( ct ) is string line )
        {
            var trimmedLine = line.AsSpan().Trim();
            if ( trimmedLine.IsEmpty )
                continue;

            var currentLog = JsonSerializer.Deserialize<MovementLog>( trimmedLine );
            if ( lastLog is not null && currentLog.Equals( lastLog ) )
                continue;

            yield return currentLog;
            lastLog = currentLog;
        }
    }
}