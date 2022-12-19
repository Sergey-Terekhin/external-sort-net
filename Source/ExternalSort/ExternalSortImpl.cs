using Serilog.Core;

namespace ExternalSort;

internal class ExternalSortImpl
{
    internal const int ReadBufferSize = 10 * 1024 * 1024;
    private const int StreamBufferSize = 4096;
    private const double MemoryLimitThreshold = 0.8;
    private const long FreeMemorySize = 4L * 1024L * 1024L * 1024L;
    private readonly Options _options;
    private readonly Logger _logger;

    public ExternalSortImpl(Options options, Logger logger)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task SortAsync(CancellationToken token = default)
    {
        var inputFileInfo = new FileInfo(_options.Input);

        if (!inputFileInfo.Exists)
            throw new Exception("Input file does not exist");

        var memoryInfo = GC.GetGCMemoryInfo();
        SetMemoryLimit(memoryInfo);
        SetBlockSize(inputFileInfo);
        SetBlockCount(inputFileInfo);

        _logger.Information(
            "Input file size: {InputSize} bytes" +
            "Total available memory on machine: {MemoryInfo} bytes. " +
            "Memory limit is set to {MemoryLimit} bytes. " +
            "Block size is set to {BlockSize} bytes. " +
            "Initial blocks count: {BlockCount}",
            inputFileInfo.Length, memoryInfo.TotalAvailableMemoryBytes, _options.MemoryLimit, _options.BlockSize, _options.BlockCount);


        _logger.Information("Split and sort stage");

        var isMergeRequired = await SplitAndSort(inputFileInfo, token);
        if (isMergeRequired)
        {
            _logger.Information("Merge stage");
            await Merge(token);
        }

        // if (Directory.Exists(_options.Temp))
        //     Directory.Delete(_options.Temp, true);
    }

    private void SetBlockCount(FileInfo inputFileInfo)
    {
        _options.BlockCount = (int)Math.Ceiling((double)inputFileInfo.Length / _options.BlockSize);
    }

    private async Task Merge(CancellationToken token)
    {
        var chunkBufferSize = (int)(_options.BlockSize / (_options.BlockCount + 1));

        var comparer = new FileRecordComparer();
        var contexts = _options.BlockFiles.Select(it =>
        {
            var stream = CreateReadStream(it);
            var reader = new FileRecordReader(stream, chunkBufferSize);
            return new MergeContext(reader, chunkBufferSize);
        }).ToArray();

        var outputBuffer = new FileRecord[chunkBufferSize];
        await using var writeStream = CreateWriteStream(_options.Output);
        await using var writer = new FileRecordWriter(writeStream);
        var outputIdx = 0;
        try
        {
            foreach (var reader in contexts)
            {
                await reader.NextItem(token);
            }

            while (true)
            {
                if (outputIdx == outputBuffer.Length)
                {
                    await writer.WriteAsync(outputBuffer, token);
                    Array.Clear(outputBuffer);
                    outputIdx = 0;
                }

                var minContext = FindMinContext(contexts, comparer);
                if (minContext == null)
                {
                    // all reader contexts are completed
                    if (outputIdx > 0)
                        await writer.WriteAsync(new ArraySegment<FileRecord>(outputBuffer, 0, outputIdx), token);
                    break;
                }

                outputBuffer[outputIdx++] = minContext.Current!;
                await minContext.NextItem(token);
            }
        }
        finally
        {
            foreach (var reader in contexts)
            {
                await reader.DisposeAsync();
            }
        }

        await writeStream.FlushAsync(token);
    }

    private MergeContext? FindMinContext(MergeContext[] contexts, FileRecordComparer comparer)
    {
        return contexts.Where(c => c.Current != null).MinBy(c => c.Current, comparer);
    }

    private async Task<bool> SplitAndSort(FileInfo inputFile, CancellationToken token)
    {
        await using var stream = CreateReadStream(inputFile.FullName);

        var bufferSize = (int)Math.Min(ReadBufferSize, _options.BlockSize);
        await using var reader = new FileRecordReader(stream, bufferSize);
        if (_options.BlockCount == 1)
        {
            //just read and sort file
            var records = await reader.ReadAsync(_options.BlockSize, token);
            records.Sort(FileRecordComparer.Default);
            var outputStream = CreateWriteStream(_options.Output);
            await using var writer = new FileRecordWriter(outputStream);
            await writer.WriteAsync(records, token);

            return false;
        }

        if (string.IsNullOrEmpty(_options.Temp))
            _options.Temp = Path.Combine(Path.GetTempPath(), Path.GetFileNameWithoutExtension(inputFile.Name));
        if (!Directory.Exists(_options.Temp))
            Directory.CreateDirectory(_options.Temp);

        var index = 1;

        _options.BlockFiles = new List<string>();
        do
        {
            var records = await reader.ReadAsync(_options.BlockSize, token);
            if (records.Count == 0)
                break;

            records.Sort(FileRecordComparer.Default);

            var tempFile = Path.Combine(_options.Temp,
                $"{Path.GetFileNameWithoutExtension(inputFile.Name)}_sorted_{index}{inputFile.Extension}");
            _options.BlockFiles.Add(tempFile);
            var outputStream = CreateWriteStream(tempFile);
            await using var writer = new FileRecordWriter(outputStream);
            await writer.WriteAsync(records, token);
            index++;
        } while (true);

        return true;
    }

    private static FileStream CreateWriteStream(string tempFile)
    {
        return new FileStream(tempFile, FileMode.Create, FileAccess.Write, FileShare.None, StreamBufferSize,
            FileOptions.Asynchronous);
    }

    private static FileStream CreateReadStream(string path)
    {
        return new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            StreamBufferSize,
            FileOptions.Asynchronous | FileOptions.SequentialScan);
    }

    private void SetBlockSize(FileInfo inputFileInfo)
    {
        if (_options.BlockSize <= 0 || _options.BlockSize > _options.MemoryLimit)
            _options.BlockSize = Math.Min(inputFileInfo.Length, _options.MemoryLimit);
    }

    private void SetMemoryLimit(GCMemoryInfo memoryInfo)
    {
        var maxMemory = memoryInfo.TotalAvailableMemoryBytes;
        var memoryLimit = (long)Math.Max(MemoryLimitThreshold * maxMemory, maxMemory - FreeMemorySize);
        if (_options.MemoryLimit <= 0 || _options.MemoryLimit > memoryLimit)
            _options.MemoryLimit = memoryLimit;
    }

    private class MergeContext : IAsyncDisposable
    {
        private bool _isCompleted;
        private readonly FileRecordReader _reader;
        private readonly SemaphoreSlim _wait = new SemaphoreSlim(1, 1);
        private readonly int _readSize;
        private IReadOnlyList<FileRecord>? _data;
        private int _dataIdx;

        public MergeContext(FileRecordReader reader, int readSize)
        {
            _reader = reader;
            _readSize = readSize;
        }

        public ValueTask DisposeAsync()
        {
            _wait.Dispose();
            return _reader.DisposeAsync();
        }

        public FileRecord? Current { get; private set; }

        public async Task NextItem(CancellationToken token = default)
        {
            if (_data != null && _dataIdx < _data.Count)
            {
                Current = _data[_dataIdx++];
                return;
            }

            if (_isCompleted)
            {
                Current = null;
                return;
            }

            _data = await _reader.ReadAsync(_readSize, token);
            if (_data.Count == 0)
            {
                _isCompleted = true;
                Current = null;
                return;
            }

            _dataIdx = 0;
            Current = _data[_dataIdx++];
        }
    }
}