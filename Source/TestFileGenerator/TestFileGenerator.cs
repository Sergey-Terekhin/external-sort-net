using System.Buffers;
using System.Threading.Tasks.Dataflow;
using Serilog.Core;

using DisposableArraySegment = ExternalSort.DisposableArraySegment<byte>;
// ReSharper disable ForCanBeConvertedToForeach
// ReSharper disable ArrangeObjectCreationWhenTypeNotEvident

namespace ExternalSort;

internal class TestFileGenerator
{

    private const int FileBufferSize = 8*1024*1024;
    private static readonly int MaxAdditionalLength = Constants.MaxLongDigits + Constants.SeparatorLength + Constants.EndOfLineLength;

    private readonly Options _options;
    private readonly Logger _logger;

    public TestFileGenerator(Options options, Logger logger)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger;
    }

    public async Task GenerateFileAsync()
    {
        _logger.Information("Started to generate test data file {OutputFile}", _options.Output);
        var context = new GenerationContext(_options);
        context.DataForDuplicates = GenerateRandomStringsData(context);
        _logger.Information("Generated {DataForDuplicates} records to use as duplicates source", context.DataForDuplicates.Length);

        var concurrency = _options.Concurrency <= 0 ? DataflowBlockOptions.Unbounded : _options.Concurrency;
        var parallelOptions = new ExecutionDataflowBlockOptions
        {
            MaxDegreeOfParallelism = concurrency,
            BoundedCapacity = 8192 * 10 / context.MaximalDataLength
        };

        var generateRandomStringBlock = new TransformBlock<GenerationContext, RecordContext>(GenerateLineData, parallelOptions);
        var batchBlock = new BatchBlock<RecordContext>(FileBufferSize / (context.MaximalDataLength + MaxAdditionalLength));
        var combineBuffersBlock = new TransformBlock<RecordContext[], BufferContext>(CombineBuffers);
        var writeToFileBlock = new ActionBlock<BufferContext>(WriteLineToStream, new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = 1 });

        var linkOptions = new DataflowLinkOptions { PropagateCompletion = true };

        generateRandomStringBlock.LinkTo(batchBlock, linkOptions);
        batchBlock.LinkTo(combineBuffersBlock, linkOptions);
        combineBuffersBlock.LinkTo(writeToFileBlock, linkOptions);

        while (context.GeneratedBytes < _options.Size)
        {
            await generateRandomStringBlock.SendAsync(context);
        }

        generateRandomStringBlock.Complete();

        await writeToFileBlock.Completion;

        await context.WriteStream.FlushAsync();
        context.Dispose();

        _logger.Information(
            "Finished generation of test data file. Generated {GeneratedRecords} records and {GeneratedBytes} bytes. Duplicates written: {DuplicatesCount}",
            context.GeneratedRecords,
            context.GeneratedBytes,
            context.DuplicatesHit);
    }

    private static BufferContext CombineBuffers(RecordContext[] buffers)
    {
        var resultBuffer = ArrayPool<byte>.Shared.Rent(FileBufferSize);
       
        var writtenLength = 0;
        for (var i = 0; i < buffers.Length; i++)
        {
            var span = resultBuffer.AsSpan(writtenLength);
            var buffer = buffers[i].Data;
            writtenLength += buffer.WriteTo(span);
            buffer.Dispose();
        }

        return new(
            buffers[0].GlobalContext,
            new DisposableArraySegment(
                new ArraySegment<byte>(resultBuffer, 0, writtenLength),
                array => ArrayPool<byte>.Shared.Return(array)));
    }

    private static async Task WriteLineToStream(BufferContext ctx)
    {
        var stream = ctx.GlobalContext.WriteStream;
        var line = ctx.Data.Value;
        await stream.WriteAsync(line);
        ctx.Data.Dispose();
    }

    private static RecordContext GenerateLineData(GenerationContext ctx)
    {
        var num = ctx.RandomNumber();
        DisposableArraySegment data;
        
        var takeDuplicate = ctx.Random.NextDouble() <= ctx.DuplicatesRatio;
        if (takeDuplicate && ctx.DataForDuplicates.Length > 0)
        {
            ctx.IncrementDuplicateHit();
            data = ctx.RandomDuplicate();
        }
        else
        {
            var chars = ArrayPool<byte>.Shared.Rent(ctx.MaximalDataLength);
            var dataLength = ctx.Random.Next(ctx.MinimalDataLength, ctx.MaximalDataLength);
            FillRandomArray(chars, 0, dataLength, ctx.Random);
            data = new DisposableArraySegment(
                new ArraySegment<byte>(chars, 0, dataLength),
                arr => ArrayPool<byte>.Shared.Return(arr));
        }

        var record = new FileRecord(num, data);
        ctx.IncrementGeneratedBytes(record.ByteLength());
        ctx.IncrementGeneratedRecords();
        return new(ctx, record);
    }

    private static DisposableArraySegment[] GenerateRandomStringsData(GenerationContext ctx)
    {
        if (ctx.PreGeneratedDatasetSize == 0)
            return Array.Empty<DisposableArraySegment>();

        var result = new DisposableArraySegment[ctx.PreGeneratedDatasetSize];
        for (var i = 0; i < result.Length; i++)
        {
            var length = ctx.Random.Next(ctx.MinimalDataLength, ctx.MaximalDataLength);
            var array = new byte[length];
            FillRandomArray(array, 0, length, ctx.Random);
            result[i] = new DisposableArraySegment(array);
        }

        return result;
    }

    private static void FillRandomArray(byte[] array, int start, int length, Random random)
    {
        array[start] = (byte)Constants.AllowedFirstChars[random.Next(0, Constants.AllowedFirstChars.Length)];
        if (length == 1)
            return;

        for (var i = 1; i < length; i++)
            array[i + start] = (byte)Constants.AllowedChars[random.Next(0, Constants.AllowedChars.Length)];
    }

    private record RecordContext(GenerationContext GlobalContext, FileRecord Data);
    private record BufferContext(GenerationContext GlobalContext, DisposableArraySegment<byte> Data);


    private record GenerationContext : IDisposable
    {
        private long _generatedBytes;
        private long _generatedRecords;
        private long _duplicatesHit;
        private const int MinStringLength = 1;
        private const int MaxDuplicatesDatasetSize = 10000;

        public GenerationContext(Options options)
        {
            Random = Random.Shared;
            MaximalDataLength = options.StringLength;
            MinimalDataLength = MinStringLength;
            MedianDataLength = (MaximalDataLength - MinimalDataLength) / 2 + MaxAdditionalLength;
            ApproximateRecordsCount = options.Size / MedianDataLength;
            PreGeneratedDatasetSize = (int)Math.Min(ApproximateRecordsCount * options.DuplicatesRatio, MaxDuplicatesDatasetSize);
            DuplicatesRatio = options.DuplicatesRatio;
            WriteStream = new FileStream(options.Output, new FileStreamOptions
            {
                Mode = FileMode.Create,
                Options = FileOptions.Asynchronous | FileOptions.SequentialScan,
                Access = FileAccess.Write
            });
        }

        /// <summary> Probability that random data will be taken from pre-generated array to provide duplicates </summary>
        public double DuplicatesRatio { get; }

        /// <summary> Returns randomizer to use </summary>
        public Random Random { get; }

        /// <summary> Amount of generated bytes </summary>
        public long GeneratedBytes => _generatedBytes;

        /// <summary> Amount of generated records </summary>
        public long GeneratedRecords => _generatedRecords;

        /// <summary> Amount of strings taken from pre-generated array </summary>
        public long DuplicatesHit => _duplicatesHit;

        /// <summary> Approximate amount of generated records </summary>
        public long ApproximateRecordsCount { get; }

        /// <summary> Amount of record in the pre-generated random data used as source for duplicates </summary>
        public int PreGeneratedDatasetSize { get; }

        /// <summary> Median data length </summary>
        public int MedianDataLength { get; }

        /// <summary> Minimal data length </summary>
        public int MinimalDataLength { get; }

        /// <summary> Maximal data length </summary>
        public int MaximalDataLength { get; }

        public DisposableArraySegment[] DataForDuplicates { get; set; } = Array.Empty<DisposableArraySegment>();

        /// <summary> Stream to write generated data </summary>
        public Stream WriteStream { get; }


        /// <summary> Increment generated bytes count by provided value </summary>
        /// <param name="count">Increment value</param>
        public void IncrementGeneratedBytes(int count) => Interlocked.Add(ref _generatedBytes, count);

        /// <summary> Increment generated bytes count by provided value </summary>
        public void IncrementGeneratedRecords() => Interlocked.Increment(ref _generatedRecords);

        /// <summary> Increment value of amount of strings taken from pre-generated array </summary>
        public void IncrementDuplicateHit() => Interlocked.Increment(ref _duplicatesHit);


        public DisposableArraySegment RandomDuplicate()
        {
            return DataForDuplicates[Random.Next(0, DataForDuplicates.Length)];
        }

        public void Dispose()
        {
            WriteStream.Dispose();
        }

        public long RandomNumber() =>
            Random.NextInt64(1000) *
            Random.NextInt64(1000) *
            Random.NextInt64(1000) *
            Random.NextInt64(1000);
    }
}