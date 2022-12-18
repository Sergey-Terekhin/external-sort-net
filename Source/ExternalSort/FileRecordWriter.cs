using System.Buffers;

namespace ExternalSort;

public class FileRecordWriter : IDisposable, IAsyncDisposable
{
    private const int FileBufferSize = 1024 * 1024;
    private readonly Stream _writeStream;
    private readonly bool _disposeStream;

    public FileRecordWriter(Stream writeStream, bool disposeStream = true)
    {
        _writeStream = writeStream ?? throw new ArgumentNullException(nameof(writeStream));
        _disposeStream = disposeStream;
        if (!_writeStream.CanWrite) throw new InvalidOperationException("Stream does not support writing");
    }

    public async Task WriteAsync(IReadOnlyList<FileRecord> records, CancellationToken token = default)
    {
        var resultBuffer = ArrayPool<byte>.Shared.Rent(FileBufferSize);

        try
        {
            var i = 0;
            while (i < records.Count)
            {
                var writtenLength = 0;
                while (i < records.Count)
                {
                    var record = records[i];
                    if (writtenLength + record.ByteLength() > resultBuffer.Length)
                        break;

                    var span = resultBuffer.AsMemory(writtenLength);
                    writtenLength += record.WriteTo(span.Span);
                    i++;
                }

                await _writeStream.WriteAsync(resultBuffer.AsMemory(0, writtenLength), token);
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(resultBuffer);
        }
       
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposeStream) _writeStream.Dispose();
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        return _disposeStream ? _writeStream.DisposeAsync() : ValueTask.CompletedTask;
    }
}