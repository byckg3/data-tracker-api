
using System.Collections.Concurrent;
using System.Text;
using DataTrackerApi.Infrastructure.Settings;
using DataTrackerApi.Models;

namespace DataTrackerApi.Services;

public class ClientFileManager : IAsyncDisposable
{
    private readonly ConcurrentDictionary<string, FileStream> _writers = [];
    private readonly ILogger<ClientFileManager> _logger;
    public string BaseDir { get; set; } = FileSettings.ClientBaseDirectory;

    public ClientFileManager( ILogger<ClientFileManager> logger )
    {
        _logger = logger;
    }

    public async Task WriteToClientFileAsync( ClientMessage clientMessage, CancellationToken cancellationToken = default )
    {
        if ( !clientMessage.IsConnected && _writers.ContainsKey( clientMessage.Id ) )
        {
            await CloseAsync( clientMessage.Id );
            _logger.LogInformation( "Client {Id} has left.", clientMessage.Id );

            return;
        }

        if ( !_writers.ContainsKey( clientMessage.Id ) )
        {
            var fs = CreateFileStream( clientMessage.Id );
            if ( !_writers.TryAdd( clientMessage.Id, fs ) )
            {
                // Handle the case where the writer could not be added
                await fs.DisposeAsync();
            }
        }
        await WriteLineAsync(
                clientMessage.Id, clientMessage.Payload, cancellationToken );
    }

    public async Task FlushAllAsync( CancellationToken cancellationToken = default )
    {
        foreach ( var ( id, writer ) in _writers )
        {
            try
            {
                await writer.FlushAsync( cancellationToken );
            }
            catch ( Exception ex )
            {
                _logger.LogError( ex, "Error flushing file stream for client ID: {Id}", id );
            }
        }
    }

    private FileStream CreateFileStream( string connectionId )
    {
        string clientDir = connectionId;
        string folderPath = Path.Combine( BaseDir, clientDir );
        Directory.CreateDirectory( folderPath );

        string fileName = DateTime.UtcNow.ToString( FileSettings.ClientFileNameFormat );
        string fullPath = Path.Combine( folderPath, $"{fileName}.txt" );

        return new FileStream(
            fullPath, FileMode.Append, FileAccess.Write, FileShare.Read, 4096, true );
    }

    private async Task WriteLineAsync( string connectionId, Memory<byte> data, CancellationToken cancellationToken = default )
    {
        if ( _writers.TryGetValue( connectionId, out var writer ) )
        {
            await writer.WriteAsync( data , cancellationToken );
            await writer.WriteAsync(
                Encoding.UTF8.GetBytes( Environment.NewLine ) , cancellationToken );

            // await writer.FlushAsync( cancellationToken );
        }
    }

    private async Task CloseAsync( string connectionId )
    {
        if ( _writers.TryRemove( connectionId, out var writer ) )
        {
            await writer.DisposeAsync();
        }
    }

    public async Task CloseAllAsync()
    {
        foreach ( var ( id, writer ) in _writers )
        {
            await writer.DisposeAsync();
        }
        _writers.Clear();
    }

    private void LogMessage( string connectionId, Memory<byte> data )
    {
        string payload = Encoding.UTF8.GetString( data.Span );
        using ( Serilog.Context.LogContext.PushProperty( "ConnId", connectionId ) )
        {
            _logger.LogInformation( "{Payload}", payload );
        }
    }

    public async ValueTask DisposeAsync()
    {
        await CloseAllAsync();
    }
}