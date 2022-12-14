// See https://aka.ms/new-console-template for more information

using Serilog;
using Serilog.Core;
using TestFileGenerator;

internal static class Program
{
    public static async Task<int> Main(string[] args)
    {
        var logger = new LoggerConfiguration().WriteTo.Console().CreateLogger();
        // await Parser.Default.ParseArguments<Options>(args)
        //     .WithParsedAsync(opt=>new TestFileGenerator(opt, logger).GenerateFileAsync());
        // await new TestFileGenerator.TestFileGenerator(new Options
        // {
        //     Concurrency = -1,
        //     Output = @"C:\Users\terekhin\Downloads\output.txt",
        //     Size = 1024 * 1024 * 1024,
        //     StringLength = 512,
        //     DuplicatesRatio = 0.01
        // }, logger).GenerateFileAsync();

        foreach (var length in new[] { 20, 100, 500, 1000 })
        {
            logger.Information("Creating 1Gb file for string length {Length} bytes", length);
            await new TestFileGenerator.TestFileGenerator(new Options
            {
                Concurrency = -1,
                Output = $"C:\\Users\\terekhin\\Downloads\\output_{length}.txt",
                Size = 1024 * 1024 * 1024,
                StringLength = length,
                DuplicatesRatio = 0.01
            }, logger).GenerateFileAsync();
        }


        return 0;
    }
}