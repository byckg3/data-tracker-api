using DataTrackerApi.Infrastructure.Settings;
using DataTrackerApi.Models;
using DataTrackerApi.Services;
using Microsoft.Extensions.Logging;

namespace DataTrackerApi.Tests.Services;

public class PlaybackServiceTests
{
    private readonly PlaybackService _playbackService;

    public PlaybackServiceTests()
    {

        // Console.WriteLine( FileSettings.ProjectDirectory );

        // var logger = NullLogger<PlaybackService>.Instance;
        using var loggerFactory = LoggerFactory.Create( builder =>
        {
            builder.AddConsole();
        } );
        var logger = loggerFactory.CreateLogger<PlaybackService>();
        _playbackService = new PlaybackService(logger)
        {
            BaseDir = FileSettings.ProjectDirectory
        };
    }

    [Fact]
    [Trait( "Tag", "TestOnly" )]
    public async Task StreamLogAsync_ShouldStreamLogs()
    {
        var connectionId = "d97ddfbd-7c59-4bf7-8b5d-ac555fff3ede";
        // // Create a sample log file for testing
        // Directory.CreateDirectory( connectionId );
        // await File.WriteAllLinesAsync( targetFilePath, new[] { "Log entry 1", "Log entry 2", "Log entry 3" } );

        var cts = new CancellationTokenSource();
        var ( isSuccess, error, logStream ) = _playbackService.PrepareStream( connectionId, "2026050618" );

        var numberOfLogsToRead = 3;
        var logs = new List<MovementLog>();

        Assert.True( isSuccess, $"Expected success but got error: {error}" );
        Assert.NotNull( logStream );
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