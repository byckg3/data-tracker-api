using System.Collections.Concurrent;
using System.Text;
using DataTrackerApi.Infrastructure.Settings;
using DataTrackerApi.Models;
using Microsoft.Extensions.Caching.Memory;

namespace DataTrackerApi.Services;

public class ClientFileManager : IAsyncDisposable
{
    private readonly ConcurrentDictionary<string, FileContext> _fileContexts = [];
    private readonly IMemoryCache _cache;
    private readonly ILogger<ClientFileManager> _logger;
    private readonly int BufferSize = 1024 * 8;
    private static readonly byte[] _newlineBytes = Encoding.UTF8.GetBytes( Environment.NewLine );
    public string BaseDir { get; set; } = FileSettings.ClientBaseDirectory;

    public ClientFileManager( ILogger<ClientFileManager> logger )
    {
        _logger = logger;
        _cache = new MemoryCache( new MemoryCacheOptions
        {
            SizeLimit = 1000, // Set a size limit for the cache (optional)
            ExpirationScanFrequency = TimeSpan.FromMinutes( 5 ) // Set how often to scan for expired items (optional)
        } );
    }

    public async Task WriteToClientFileAsync( ClientMessage client, CancellationToken ct = default )
    {
        if ( !_fileContexts.ContainsKey( client.Id ) )
        {
            var fileContext = CreateFileContext( client.Id );
            var added = _fileContexts.TryAdd( client.Id, fileContext );
            if ( !added )
            {
                // Handle the case where the writer could not be added
                await fileContext.DisposeAsync();
            }
        }
        await WriteLineAsync(
                client.Id, client.Payload, ct );

        if ( !client.IsConnected )
        {
            await CloseAsync( client.Id );
            _logger.LogInformation( "Client {Id} has left.", client.Id );
            // return;
        }
    }

    public async Task FlushAllAsync( CancellationToken ct = default )
    {
        foreach ( var ( id, fileContext ) in _fileContexts )
        {
            try
            {
                await fileContext.FlushAsync( ct );
            }
            catch ( Exception ex )
            {
                _logger.LogError( ex, "Error flushing file stream for client ID: {Id}", id );
            }
        }
    }

    public async Task<int> CleanExpiredAsync( TimeSpan expireAfter, CancellationToken ct = default )
    {
        int removed = 0;
        foreach ( var ( id, ctx ) in _fileContexts )
        {
            if ( ct.IsCancellationRequested )
                break;

            if ( ctx.IsExpired( expireAfter ) )
            {
                if ( _fileContexts.TryRemove( id, out var fileContext ) )
                {
                    await fileContext.DisposeAsync();
                    removed++;
                }
            }
        }
        return removed;
    }

    private FileStream CreateFileStream( string id )
    {
        string clientDir = id;
        string folderPath = Path.Combine( BaseDir, clientDir );
        Directory.CreateDirectory( folderPath );

        string fileName = DateTime.UtcNow.ToString( FileSettings.ClientFileNameFormat );
        string fullPath = Path.Combine( folderPath, $"{fileName}.txt" );

        return new FileStream(
            fullPath, FileMode.Append, FileAccess.Write, FileShare.Read, BufferSize, true );
    }

    private FileContext CreateFileContext( string id )
    {
        var fs = CreateFileStream( id );
        return new FileContext( fs );
    }

    private async Task WriteLineAsync( string id, Memory<byte> data, CancellationToken ct = default )
    {
        if ( _fileContexts.TryGetValue( id, out var fileContext ) )
        {
            await fileContext.WriteAsync( data , ct );
            await fileContext.WriteAsync( _newlineBytes , ct );
            // await fileContext.FlushAsync( ct );
        }
    }

    private async Task CloseAsync( string id )
    {
        if ( _fileContexts.TryRemove( id, out var fileContext ) )
        {
            await fileContext.DisposeAsync();
        }
    }

    public async Task CloseAllAsync()
    {
        foreach ( var ( id, fileContext ) in _fileContexts )
        {
            await fileContext.DisposeAsync();
        }
        _fileContexts.Clear();
    }

    private void LogMessage( string id, Memory<byte> data )
    {
        string payload = Encoding.UTF8.GetString( data.Span );
        using ( Serilog.Context.LogContext.PushProperty( "ConnId", id ) )
        {
            _logger.LogInformation( "{Payload}", payload );
        }
    }

    public async ValueTask DisposeAsync()
    {
        await CloseAllAsync();
    }
}