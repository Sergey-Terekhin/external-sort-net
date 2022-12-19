using System.Buffers;
using System.Text;
using DisposableArray = ExternalSort.DisposableArraySegment<ExternalSort.FileRecord>;

namespace ExternalSort;

internal class FileRecordReader : IDisposable, IAsyncDisposable
{
    private static readonly int MinLineLength = 1 + 1 + Constants.SeparatorLength + Constants.EndOfLineLength;
    private readonly Stream _stream;
    private readonly int _readBufferSize;
    private readonly bool _disposeStream;
    private readonly ArrayPool<FileRecord> _pool = ArrayPool<FileRecord>.Shared;

    private readonly Memory<byte> _buffer;
    private Memory<byte> _readBuffer;
    private int _remainingBytes;

    public FileRecordReader(Stream stream, int readBufferSize, bool disposeStream = true)
    {
        if (readBufferSize <= 0) throw new ArgumentOutOfRangeException(nameof(readBufferSize));
        _stream = stream ?? throw new ArgumentNullException(nameof(stream));
        _readBufferSize = readBufferSize;
        _disposeStream = disposeStream;
        _buffer = new(new byte[readBufferSize]);
        _readBuffer = _buffer;
    }

    public async Task<List<FileRecord>> ReadAsync(long size, CancellationToken token = default)
    {
        var result = new List<FileRecord>((int)(Math.Min(_stream.Length, size) / MinLineLength + 1));
        var readBytes = 0L;
        while (readBytes < size)
        {
            var bytesToRead = (int)Math.Min(size - readBytes, _readBuffer.Length);
            if (bytesToRead < Constants.MaxLineLength)
                bytesToRead = Math.Min(_readBuffer.Length, Constants.MaxLineLength);

            var readBuffer = _readBuffer[..bytesToRead];
            var readChunkCount = await _stream.ReadAsync(readBuffer, token);
            if (readChunkCount == 0)
                break;

            var recordsBuffer = _buffer[..(readChunkCount + _remainingBytes)];
            using var records = ReadFrom(recordsBuffer, out var lastCompleteRecordByte, out var lastRecordCompleted);

            if (records.Value.Count > 0)
            {
                result.AddRange(records.Value);
                readBytes += lastCompleteRecordByte;
            }

            if (!lastRecordCompleted)
            {
                //copy not completed record data to the beginning of the buffer
                var slice = recordsBuffer[lastCompleteRecordByte..];
                _remainingBytes = slice.Length;
                slice.CopyTo(_buffer);
                //reduce available buffer to read after copied data
                _readBuffer = _buffer[_remainingBytes..];
            }
        }

        return result;
    }

    internal DisposableArray ReadFrom(ReadOnlyMemory<byte> readFrom, out int lastCompleteRecordIndex, out bool lastRecordCompleted)
    {
        lastCompleteRecordIndex = 0;
        lastRecordCompleted = false;
        var recordIndex = 0;

        var bytes = readFrom.Span;
        var array = _pool.Rent(_readBufferSize / MinLineLength + 1);

        var recordStart = 0;
        for (var i = 0; i < bytes.Length; i++)
        {
            if (bytes[i] != '\r')
                continue;

            if (i < bytes.Length - 1)
            {
                i++;
                if (bytes[i] == '\n')
                {
                    //found end of record, try to parse
                    lastCompleteRecordIndex = i;
                    array[recordIndex++] = ParseRecord(bytes[recordStart..(i - Constants.EndOfLineLength + 1)]);
                    recordStart = i + 1;
                }
                else
                {
                    throw new InvalidOperationException("Unable to parse EOL. \\r char must be followed by \\n");
                }
            }
            else
            {
                //incomplete record
                break;
            }
        }

        return recordIndex == 0
            ? new DisposableArray(ArraySegment<FileRecord>.Empty)
            : new DisposableArray(new ArraySegment<FileRecord>(array, 0, recordIndex), x => _pool.Return(x));
    }

    private FileRecord ParseRecord(ReadOnlySpan<byte> line)
    {
        var separatorIndex = line.IndexOf(Constants.Separator);
        if (separatorIndex == -1)
            throw new InvalidOperationException($"Unable to find separator in the line {Encoding.ASCII.GetString(line)}");

        Span<char> numChars = stackalloc char[Constants.MaxLongDigits];
        Encoding.ASCII.GetChars(line[..separatorIndex], numChars);

        var currentNumber = long.TryParse(numChars, out var value)
            ? value
            : throw new InvalidOperationException($"Unable to parse number value from string {new string(numChars)}");

        var stringData = line[(separatorIndex + Constants.SeparatorLength)..];

        return new FileRecord(currentNumber, new DisposableArraySegment<byte>(stringData.ToArray()));
    }


    public void Dispose()
    {
        if (_disposeStream)
            _stream.Dispose();
    }

    public ValueTask DisposeAsync()
    {
        return _disposeStream ? _stream.DisposeAsync() : ValueTask.CompletedTask;
    }
}