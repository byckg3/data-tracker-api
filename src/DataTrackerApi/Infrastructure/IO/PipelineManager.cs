using System.IO.Pipelines;

public class PipelineManager : IDisposable
{
    private readonly Pipe _pipe = new();
    private readonly SemaphoreSlim _lock = new(1, 1);

    public PipeReader GetReader() => _pipe.Reader;
    public PipeWriter GetWriter() => _pipe.Writer;

    public async Task ResetAsync()
    {
        await _lock.WaitAsync();
        try
        {
            await _pipe.Reader.CompleteAsync();
            await _pipe.Writer.CompleteAsync();

            _pipe.Reset();
        }
        finally
        {
            _lock.Release();
        }
    }

    public void Dispose()
    {
        _pipe.Reader.Complete();
        _pipe.Writer.Complete();
        _lock.Dispose();
    }
}