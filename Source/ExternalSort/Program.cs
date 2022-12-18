using System.Diagnostics;
using CommandLine;
using ExternalSort;
using FluentValidation;
using Serilog;

var logger = new LoggerConfiguration().WriteTo.Console().CreateLogger();
var result = Parser.Default.ParseArguments<Options>(args);
if (result is Parsed<Options>)
{
    try
    {
        var options = result.Value;
        var start = Stopwatch.GetTimestamp();
        await new OptionsValidator().ValidateAndThrowAsync(options);
        await new ExternalSortImpl(options, logger).SortAsync();
        var elapsed = TimeSpan.FromTicks(Stopwatch.GetTimestamp() - start);
        logger.Information(
            "Finished sorting of the file {InputFile}. Results are stored in {OutputFile}. Elapsed time: {Elapsed}",
            options.Input, options.Output, elapsed);
        return 0;
    }
    catch (Exception e)
    {
        logger.Fatal(e, "Failed to sort file. Options were:\r\n{@Options}\r\n", result.Value);
        return -1;
    }
}

return -2;