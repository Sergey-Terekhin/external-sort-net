namespace TestFileGenerator;

internal class Options
{
    [CommandLine.Option('s', "size", HelpText = "Size of the generated file")]
    public int Size { get; set; }

    [CommandLine.Option('o', "output", HelpText = "Path to the generated file")]
    public string Output { get; set; } = null!;
    
    [CommandLine.Option('l',"string-length", Default = 1024, HelpText = "Maximal string length of generated records")]
    public int StringLength { get; set; }
    
    [CommandLine.Option('c',"concurrency", Default = -1, HelpText = "Degree of parallelism used to generate test data")]
    public int Concurrency { get; set; }
    
    [CommandLine.Option('d',"duplicates-ratio", Default = 0.01, HelpText = "Probability that random data will be taken from pre-generated array to provide duplicates. Default value is 1%")]
    public double DuplicatesRatio { get; set; }
}