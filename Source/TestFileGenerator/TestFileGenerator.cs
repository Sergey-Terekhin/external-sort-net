using System.Buffers;
using System.Text;
using System.Threading.Tasks.Dataflow;
using Serilog.Core;

namespace TestFileGenerator;

internal class TestFileGenerator
{
    private const int MaxLongDigits = 19; //digits count in the long.MaxValue value (9,223,372,036,854,775,807)

    private const int FileBufferSize = 8192;
    private const string AllowedChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789 ";
    private const string AllowedFirstChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
    private static readonly byte[] Separator = Encoding.ASCII.GetBytes(". ");
    private static readonly byte[] EndOfLine = Encoding.ASCII.GetBytes("\r\n");
    private static readonly int SeparatorLength = Separator.Length;
    private static readonly int EndOfLineLength = EndOfLine.Length;
    private static readonly int AdditionalLength = MaxLongDigits + SeparatorLength + EndOfLineLength;

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
            BoundedCapacity = 8192 * 10 / context.MaximalDataLength,
            //MaxMessagesPerTask = -1
        };

        var generateRandomStringBlock = new TransformBlock<GenerationContext, RecordContext>(GenerateLineData, parallelOptions);
        var batchBlock = new BatchBlock<RecordContext>(FileBufferSize / (context.MaximalDataLength + AdditionalLength));
        var combineBuffersBlock = new TransformBlock<RecordContext[], RecordContext>(CombineBuffers);

        var writeToFileBlock = new ActionBlock<RecordContext>(WriteLineToStream, new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = 1 });

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

    private static RecordContext CombineBuffers(RecordContext[] buffers)
    {
        var resultBuffer = ArrayPool<byte>.Shared.Rent(FileBufferSize);
        var writtenLength = 0;
        for (var i = 0; i < buffers.Length; i++)
        {
            var buffer = buffers[i].Data;
            buffer.Value.CopyTo(resultBuffer, writtenLength);
            writtenLength += buffer.Value.Count;
            buffer.Dispose();
        }

        return new(
            buffers[0].GlobalContext,
            new DisposableWrapper(
                new ArraySegment<byte>(resultBuffer, 0, writtenLength),
                array => ArrayPool<byte>.Shared.Return(array)));
    }

    private static async Task WriteLineToStream(RecordContext ctx)
    {
        var stream = ctx.GlobalContext.WriteStream;
        var line = ctx.Data.Value;
        await stream.WriteAsync(line);
        ctx.Data.Dispose();
    }

    private static RecordContext GenerateLineData(GenerationContext ctx)
    {
        var chars = ArrayPool<byte>.Shared.Rent(ctx.MaximalDataLength + AdditionalLength);
        var num = ctx.RandomNumber();
        var recordLength = WriteLongStringBytes(num, chars.AsSpan());
        Array.Copy(Separator, 0, chars, recordLength, SeparatorLength);
        recordLength += SeparatorLength;

        var takeDuplicate = ctx.Random.NextDouble() <= ctx.DuplicatesRatio;
        if (takeDuplicate && ctx.DataForDuplicates.Length > 0)
        {
            ctx.IncrementDuplicateHit();
            return new(ctx, ctx.RandomDuplicate());
        }

        var dataLength = ctx.Random.Next(ctx.MinimalDataLength, ctx.MaximalDataLength);

        FillRandomArray(chars, recordLength, dataLength, ctx.Random);
        recordLength += dataLength;

        Array.Copy(EndOfLine, 0, chars, recordLength, EndOfLineLength);
        recordLength += EndOfLineLength;

        ctx.IncrementGeneratedBytes(recordLength);
        ctx.IncrementGeneratedRecords();

        return new(
            ctx,
            new DisposableWrapper(
                new ArraySegment<byte>(chars, 0, recordLength),
                arr => ArrayPool<byte>.Shared.Return(arr)));
    }

    private static DisposableWrapper[] GenerateRandomStringsData(GenerationContext ctx)
    {
        if (ctx.PreGeneratedDatasetSize == 0)
            return Array.Empty<DisposableWrapper>();

        var result = new DisposableWrapper[ctx.PreGeneratedDatasetSize];
        for (var i = 0; i < result.Length; i++)
        {
            var length = Random.Shared.Next(ctx.MinimalDataLength, ctx.MaximalDataLength);
            var array = new byte[length];
            FillRandomArray(array, 0, length, ctx.Random);
            result[i] = new DisposableWrapper(array);
        }

        return result;
    }

    private static void FillRandomArray(byte[] array, int start, int length, Random random)
    {
        array[start] = (byte)AllowedFirstChars[random.Next(0, AllowedFirstChars.Length)];
        if (length == 1)
            return;

        for (var i = 1; i < length; i++)
            array[i + start] = (byte)AllowedChars[random.Next(0, AllowedChars.Length)];
    }

    private static int WriteLongStringBytes(long num, Span<byte> writeTo)
    {
        return Encoding.ASCII.GetBytes(num.ToString(), writeTo);
    }

    private class RecordContext
    {
        public RecordContext(GenerationContext globalContext, DisposableWrapper data)
        {
            GlobalContext = globalContext;
            Data = data;
        }

        public GenerationContext GlobalContext { get; }
        public DisposableWrapper Data { get; }
    }

    private class DisposableWrapper : IDisposable
    {
        private readonly Action<byte[]>? _dispose;

        public DisposableWrapper(ArraySegment<byte> value, Action<byte[]>? dispose = null)
        {
            _dispose = dispose;
            Value = value;
        }

        public ArraySegment<byte> Value { get; }

        public void Dispose()
        {
            _dispose?.Invoke(Value.Array!);
        }
    }

    private record GenerationContext : IDisposable
    {
        private long _generatedBytes;
        private long _generatedRecords;
        private long _duplicatesHit;
        private const int MinStringLength = 1;
        private const int MaxDuplicatesDatasetSize = 10000;
        private readonly Random _random = Random.Shared;

        public GenerationContext(Options options)
        {
            MaximalDataLength = options.StringLength;
            MinimalDataLength = MinStringLength;
            MedianDataLength = (MaximalDataLength - MinimalDataLength) / 2 + AdditionalLength;
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
        public Random Random => _random;

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

        public DisposableWrapper[] DataForDuplicates { get; set; } = Array.Empty<DisposableWrapper>();

        /// <summary> Stream to write generated data </summary>
        public Stream WriteStream { get; }


        /// <summary> Increment generated bytes count by provided value </summary>
        /// <param name="count">Increment value</param>
        public void IncrementGeneratedBytes(int count) => Interlocked.Add(ref _generatedBytes, count);

        /// <summary> Increment generated bytes count by provided value </summary>
        public void IncrementGeneratedRecords() => Interlocked.Increment(ref _generatedRecords);

        /// <summary> Increment value of amount of strings taken from pre-generated array </summary>
        public void IncrementDuplicateHit() => Interlocked.Increment(ref _duplicatesHit);


        public DisposableWrapper RandomDuplicate()
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