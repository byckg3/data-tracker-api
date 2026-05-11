public sealed class FileContext : IAsyncDisposable
{
    private readonly FileStream _fs;
    private readonly SemaphoreSlim _lock = new( 1, 1 );
    private readonly CancellationTokenSource _cts = new();
    private int _disposeSignal = 0;

    public FileContext( FileStream stream )
    {
        _fs = stream;
    }

    public async Task WriteAsync( Memory<byte> data, CancellationToken ct = default )
    {
        if ( Volatile.Read( ref _disposeSignal ) == 1 )
            return;

        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource( ct, _cts.Token );
        try
        {
            var lockTaken = false;
            try
            {
                await _lock.WaitAsync( linkedCts.Token );
                lockTaken = true;
                // ObjectDisposedException.ThrowIf( _disposed, this );
                await _fs.WriteAsync( data, linkedCts.Token );
            }
            finally
            {
                if ( lockTaken )
                    _lock.Release();
            }
        }
        catch ( Exception ex ) when ( ex is OperationCanceledException or ObjectDisposedException )
        { }
        catch ( Exception )
        {
            throw;
        }
    }

    public async Task FlushAsync( CancellationToken ct = default )
    {
        if ( Volatile.Read( ref _disposeSignal ) == 1 )
            return;

        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource( ct, _cts.Token );
        try
        {
            var lockTaken = false;
            try
            {
                await _lock.WaitAsync( linkedCts.Token );
                lockTaken = true;

                await _fs.FlushAsync( linkedCts.Token );
            }
            finally
            {
                if ( lockTaken )
                    _lock.Release();
            }
        }
        catch ( Exception ex ) when ( ex is OperationCanceledException or ObjectDisposedException )
        { }
        catch ( Exception )
        {
            throw;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if ( Interlocked.Exchange( ref _disposeSignal, 1 ) == 1 )
        {
            return;
        }

        _cts.Cancel();
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
