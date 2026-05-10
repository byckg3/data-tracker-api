using System.Runtime.CompilerServices;
using System.Text.Json;
using DataTrackerApi.Infrastructure.Settings;
using DataTrackerApi.Models;

namespace DataTrackerApi.Services;

public class PlaybackService
{
    public string BaseDir { get; set; } = FileSettings.ClientBaseDirectory;
    private readonly ILogger<PlaybackService> _logger;

    public PlaybackService( ILogger<PlaybackService> logger )
    {
        _logger = logger;
    }

    public ( bool IsSuccess, string? Error, IAsyncEnumerable<MovementLog>? Stream )
        PrepareStream( string connectionId, string datetime )
    {
        if ( !Guid.TryParse( connectionId, out _ ) )
            return ( false, "Invalid connection ID", null );

        if ( !DateTime.TryParseExact(
                datetime, FileSettings.ClientFileNameFormat,
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out _ ) )
            return ( false, "Invalid date format", null );

        var relativePath = Path.Combine(
                connectionId, $"{datetime}{FileSettings.ClientFileNameSuffix}" );
        var fullPath = Path.GetFullPath( Path.Combine( BaseDir, relativePath ) );

        if ( !fullPath.StartsWith(
                BaseDir + Path.DirectorySeparatorChar,
                StringComparison.OrdinalIgnoreCase ) )
            return ( false, "Invalid file path", null );

        if ( !File.Exists( fullPath ) )
        {
            return ( false, "File not found", null );
        }
        return ( true, null, StreamLogAsync( fullPath ) );
    }

    private async IAsyncEnumerable<MovementLog> StreamLogAsync(
        string fullPath, [EnumeratorCancellation] CancellationToken ct = default )
    {
        using var fs = new FileStream( fullPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite );
        using var reader = new StreamReader( fs );

        MovementLog? lastLog = null;
        while ( await reader.ReadLineAsync( ct ) is string line )
        {
            var trimmedLine = line.AsSpan().Trim();
            if ( trimmedLine.IsEmpty )
                continue;

            MovementLog currentLog;
            try
            {
                currentLog = JsonSerializer.Deserialize<MovementLog>( trimmedLine );
            }
            catch ( JsonException ex )
            {
                _logger.LogWarning( ex, "Skipping malformed log line: {Line}", trimmedLine.ToString() );
                continue;
            }

            if ( lastLog is not null && currentLog.Equals( lastLog ) )
                continue;

            yield return currentLog;
            lastLog = currentLog;
        }
    }
}