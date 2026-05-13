using static System.Threading.CancellationTokenSource;

public sealed class FileContext : IAsyncDisposable
{
    private readonly FileStream _fs;
    private readonly SemaphoreSlim _lock = new( 1, 1 );
    private readonly CancellationTokenSource _cts = new();
    private DateTime _lastAccessTime;
    private int _disposeSignal = 0;

    public FileContext( FileStream stream )
    {
        _fs = stream;
        _lastAccessTime = DateTime.UtcNow;
    }

    public async Task WriteAsync( Memory<byte> data, CancellationToken ct = default )
    {
        if ( Volatile.Read( ref _disposeSignal ) == 1 )
            return;

        try
        {
            using var linkedCts = CreateLinkedTokenSource( ct, _cts.Token );
            var lockTaken = false;
            try
            {
                await _lock.WaitAsync( linkedCts.Token );
                lockTaken = true;

                _lastAccessTime = DateTime.UtcNow;
                await _fs.WriteAsync( data, linkedCts.Token );
            }
            finally
            {
                if ( lockTaken )
                    _lock.Release();
            }
        }
        catch ( Exception ex ) when ( ex is OperationCanceledException or ObjectDisposedException ) { }
        catch ( Exception )
        {
            throw;
        }
    }

    public async Task FlushAsync( CancellationToken ct = default )
    {
        if ( Volatile.Read( ref _disposeSignal ) == 1 )
            return;

        try
        {
            using var linkedCts = CreateLinkedTokenSource( ct, _cts.Token );
            var lockTaken = false;
            try
            {
                await _lock.WaitAsync( linkedCts.Token );
                lockTaken = true;

                _lastAccessTime = DateTime.UtcNow;
                await _fs.FlushAsync( linkedCts.Token );
            }
            finally
            {
                if ( lockTaken )
                    _lock.Release();
            }
        }
        catch ( Exception ex ) when (
            ex is OperationCanceledException or ObjectDisposedException ) { }
        catch ( Exception )
        {
            throw;
        }
    }

    public bool IsExpired( TimeSpan timeout )
    {
        return ( DateTime.UtcNow - _lastAccessTime ) > timeout;
    }

    public async ValueTask DisposeAsync()
    {
        if ( Interlocked.Exchange( ref _disposeSignal, 1 ) == 1 )
        {
            return;
        }

        // Signal cancellation to any ongoing operations
        _cts.Cancel();

        // Wait for any in-progress operations to complete
        await _lock.WaitAsync();
        try
        {
            await _fs.DisposeAsync();
        }
        finally
        {
            _lock.Release();
            _lock.Dispose();
            _cts.Dispose();
        }
    }
}