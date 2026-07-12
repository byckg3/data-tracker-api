using System.Buffers;
using System.Buffers.Binary;
using System.IO.Pipelines;

namespace DataTrackerApi.Infrastructure.Tcp;

public class TcpPacketParser : BackgroundService
{
    private readonly ILogger<TcpPacketParser> _logger;
    private readonly PipelineManager _pipelineManager;

    public TcpPacketParser(PipelineManager pipelineManager, ILogger<TcpPacketParser> logger)
    {
        _pipelineManager = pipelineManager;
        _logger = logger;
    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        PipeReader? reader = null;
        while (!stoppingToken.IsCancellationRequested)
        {
            reader = _pipelineManager.GetReader();

            ReadResult result = await reader.ReadAsync(stoppingToken);
            ReadOnlySequence<byte> buffer = result.Buffer;

            while (TryReadMessage(ref buffer, out ReadOnlySequence<byte> line))
            {
                // Process the line.
                _logger.LogInformation("Received message: {Message}", line.ToArray());
            }

            // Tell the PipeReader how much of the buffer has been consumed.
            reader.AdvanceTo(buffer.Start, buffer.End);

            // Stop reading if there's no more data coming.
            if (result.IsCompleted)
            {
                break;
            }
        }
        // Mark the PipeReader as complete.
        await reader!.CompleteAsync();
    }

    private bool TryReadMessage(ref ReadOnlySequence<byte> buffer, out ReadOnlySequence<byte> message)
    {
        // 範例：假設團隊協定是「前 4 個 Byte 為 Int32，代表後面 Data 的長度」(Length-Prefixed)
        if (buffer.Length < 4)
        {
            message = default;
            return false;
        }

        // 讀取長度標頭（不移動 buffer 的讀取指標）
        Span<byte> lengthBytes = stackalloc byte[4];
        buffer.Slice(0, 4).CopyTo(lengthBytes);
        int messageLength = BinaryPrimitives.ReadInt32BigEndian(lengthBytes); // 注意網路位元組序 (Big Endian)

        // 判斷整個封包（Header + Body）是否已經全部抵達
        if (buffer.Length < 4 + messageLength)
        {
            message = default;
            return false;
        }

        // 切出完整的 Message Body
        message = buffer.Slice(4, messageLength);

        // 更新 buffer，將指標移到這個封包之後
        buffer = buffer.Slice(buffer.GetPosition(4 + messageLength));
        return true;
    }
}