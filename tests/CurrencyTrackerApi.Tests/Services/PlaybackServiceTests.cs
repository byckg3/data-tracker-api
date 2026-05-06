using CurrencyTrackerApi.Infrastructure.Settings;
using CurrencyTrackerApi.Models;
using CurrencyTrackerApi.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace CurrencyTrackerApi.Tests.Services;

public class PlaybackServiceTests
{
    private readonly PlaybackService _playbackService;

    public PlaybackServiceTests()
    {
        PlaybackService.BaseDir = FileSettings.ProjectDirectory;
        // Console.WriteLine( FileSettings.ProjectDirectory );

        // var logger = NullLogger<PlaybackService>.Instance;
        using var loggerFactory = LoggerFactory.Create( builder =>
        {
            builder.AddConsole();
        } );
        var logger = loggerFactory.CreateLogger<PlaybackService>();
        _playbackService = new PlaybackService( logger );
    }

    [Fact]
    [Trait( "Tag", "TestOnly" )]
    public async Task StreamLogAsync_ShouldStreamLogs()
    {
        var connectionId = "6b56324e-7789-4e2d-bbe8-a6b435c266af";
        var targetFilePath = Path.Combine( "logs", connectionId, "status-2026050602.log" );

        // // Create a sample log file for testing
        // Directory.CreateDirectory( connectionId );
        // await File.WriteAllLinesAsync( targetFilePath, new[] { "Log entry 1", "Log entry 2", "Log entry 3" } );

        var cts = new CancellationTokenSource();
        var logStream = _playbackService.StreamLogAsync( targetFilePath, cts.Token );

        var numberOfLogsToRead = 3;
        var logs = new List<MovementLog>();
        await Assert.ThrowsAsync<TaskCanceledException>( async () =>
        {
            await foreach ( var log in logStream )
            {
                logs.Add( log );
                if ( logs.Count == numberOfLogsToRead )
                {
                    cts.Cancel(); // Stop after reading all logs
                }
            }
        } );
        Assert.Equal( numberOfLogsToRead, logs.Count );
        Assert.True( logs[ 0 ].Position.AsSpan().SequenceEqual( logs[ 1 ].Position ) );

        // // Clean up
        // File.Delete( targetFilePath );
        // Directory.Delete( connectionId );
    }
}