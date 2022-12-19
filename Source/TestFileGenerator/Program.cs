using CommandLine;
using ExternalSort;
using FluentValidation;
using Serilog;

var logger = new LoggerConfiguration().WriteTo.Console().MinimumLevel.Debug().CreateLogger();
var result = Parser.Default.ParseArguments<Options>(args);
if (result is Parsed<Options>)
{
    try
    {
        await new OptionsValidator().ValidateAndThrowAsync(result.Value);
        await new TestFileGenerator(result.Value, logger).GenerateFileAsync();
        return 0;
    }
    catch (Exception e)
    {
        logger.Fatal(e, "Failed to generate file. Options were:\r\n{@Options}\r\n", result.Value);
        return -1;
    }
}

return -2;