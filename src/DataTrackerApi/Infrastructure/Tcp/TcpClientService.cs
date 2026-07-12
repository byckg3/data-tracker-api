using System;
using System.Buffers;
using System.Buffers.Binary;
using System.IO.Pipelines;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace DataTrackerApi.Infrastructure.Tcp;

public class TcpClientService : IAsyncDisposable
{
    private Socket? _socket;
    private PipeWriter _writer;
    private CancellationTokenSource? _cts;
    private bool _isDisposed;

    private readonly ILogger _logger;

    public TcpClientService(PipeWriter writer, ILogger logger)
    {
        _writer = writer;
        _logger = logger;

        _cts = new CancellationTokenSource();
    }

    public async Task ConnectAsync(string host, int port, CancellationToken cancellationToken = default)
    {
        _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
        {
            NoDelay = true // 停用 Nagle 演算法，降低延遲
        };
        await _socket.ConnectAsync(host, port, cancellationToken);
    }

    public async Task SendAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
    {
        if (_socket is null) throw new InvalidOperationException("Client is not connected.");

        await _socket.SendAsync(data, cancellationToken);
    }

    public async Task ReceiveLoopAsync(CancellationToken cancellationToken)
    {
        if (_socket is null) throw new InvalidOperationException("Client is not connected.");
        try
        {
            const int minimumBufferSize = 512;
            while (!cancellationToken.IsCancellationRequested)
            {
                // Allocate at least 512 bytes from the PipeWriter.
                Memory<byte> memory = _writer.GetMemory(minimumBufferSize);
                try
                {
                    int bytesRead = await _socket.ReceiveAsync(
                                            memory, SocketFlags.None, cancellationToken);
                    if (bytesRead == 0)
                    {
                        break; // Socket closed
                    }
                    // Tell the PipeWriter how much was read from the Socket.
                    _writer.Advance(bytesRead);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while reading from socket.");
                    break;
                }

                // Make the data available to the PipeReader.
                FlushResult result = await _writer.FlushAsync(cancellationToken);
                if (result.IsCompleted)
                {
                    break;
                }
            }
        }
        catch (OperationCanceledException) {}
        finally
        {
            // By completing PipeWriter, tell the PipeReader that there's no more data coming.
            await _writer.CompleteAsync();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_isDisposed) return;
        _isDisposed = true;

        _cts?.Cancel();

        if (_writer != null) await _writer.CompleteAsync();

        _socket?.Close();
        _socket?.Dispose();
        _cts?.Dispose();
    }
}