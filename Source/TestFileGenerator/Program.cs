// See https://aka.ms/new-console-template for more information

using System.Buffers;
using System.Text;
using System.Threading.Tasks.Dataflow;
using TestFileGenerator;

internal static class Program
{
    private const int MinStringLength = 1;
    private const int MaxLongDigits = 19; //digits count in the long.MaxValue value (9,223,372,036,854,775,807)

    private const string AllowedChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789 ";
    private static readonly byte[] Separator = Encoding.ASCII.GetBytes(". ");
    private static readonly byte[] EndOfLine = Encoding.ASCII.GetBytes("\r\n");
    private static readonly int SeparatorLength = Separator.Length;
    private static readonly int EndOfLineLength = EndOfLine.Length;
    private static readonly int AdditionalLength = MaxLongDigits + SeparatorLength + EndOfLineLength;
    private static long GeneratedBytes;

    public static async Task<int> Main(string[] args)
    {
        /*await Parser.Default.ParseArguments<Options>(args)
            .WithParsedAsync(GenerateFileAsync);*/
        await GenerateFileAsync(new Options
        {
            Concurrency = -1,
            Output = @"C:\Users\terekhin\Downloads\output.txt",
            Size = 1024 * 1024 * 1024,
            StringLength = 512
        });

        return 0;
    }

    private static async Task GenerateFileAsync(Options options)
    {
        var stream = new FileStream(options.Output, new FileStreamOptions
        {
            Mode = FileMode.Create,
            Options = FileOptions.Asynchronous | FileOptions.SequentialScan,
            Access = FileAccess.Write
        });

        var concurrency = options.Concurrency <= 0 ? DataflowBlockOptions.Unbounded : options.Concurrency;

        var generateRandomStringBlock = new TransformBlock<int, ArraySegment<byte>>(maxSize =>
        {
            var length = Random.Shared.Next(MinStringLength, maxSize);
            var chars = ArrayPool<byte>.Shared.Rent(maxSize);
            for (var i = 0; i < length; i++)
                chars[i] = (byte)AllowedChars[Random.Shared.Next(0, AllowedChars.Length)];

            return new ArraySegment<byte>(chars, 0, length);
        }, new ExecutionDataflowBlockOptions
        {
            MaxDegreeOfParallelism = concurrency,
            BoundedCapacity = concurrency > 0 ? concurrency : Environment.ProcessorCount,
            MaxMessagesPerTask = 1
        });

        var generateRandomLineBlock = new TransformBlock<ArraySegment<byte>, ArraySegment<byte>>(chars =>
        {
            var num = Random.Shared.NextInt64(1000) * Random.Shared.NextInt64(1000);
            

            var copy = ArrayPool<byte>.Shared.Rent(chars.Count + AdditionalLength);

            var idx = GetLongStringBytes(num, copy.AsSpan());

            Array.Copy(Separator, 0, copy, idx, SeparatorLength);
            idx += SeparatorLength;

            chars.CopyTo(copy, idx);
            idx += chars.Count;

            Array.Copy(EndOfLine, 0, copy, idx, EndOfLineLength);
            idx += EndOfLineLength;

            ArrayPool<byte>.Shared.Return(chars.Array!);

            Interlocked.Add(ref GeneratedBytes, idx);

            return new ArraySegment<byte>(copy, 0, idx);
        }, new ExecutionDataflowBlockOptions
        {
            MaxDegreeOfParallelism = concurrency,
            BoundedCapacity = concurrency > 0 ? concurrency : Environment.ProcessorCount,
            MaxMessagesPerTask = 1
        });

        var writeToFileBlock = new ActionBlock<ArraySegment<byte>>(
            async line =>
            {
                await stream.WriteAsync(line);
                ArrayPool<byte>.Shared.Return(line.Array!);
            },
            new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = 1 });

        var linkOptions = new DataflowLinkOptions { PropagateCompletion = true };

        generateRandomStringBlock.LinkTo(generateRandomLineBlock, linkOptions);
        generateRandomLineBlock.LinkTo(writeToFileBlock, linkOptions);
        while (GeneratedBytes < options.Size)
        {
            await generateRandomStringBlock.SendAsync(options.StringLength);
        }

        generateRandomStringBlock.Complete();

        await writeToFileBlock.Completion;

        await stream.FlushAsync();
        await stream.DisposeAsync();
    }

    private static int GetLongStringBytes(long num, Span<byte> writeTo)
    {
        return Encoding.ASCII.GetBytes(num.ToString(), writeTo);
    }
}