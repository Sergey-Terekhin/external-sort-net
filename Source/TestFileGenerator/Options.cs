namespace ExternalSort;

// ReSharper disable once ClassNeverInstantiated.Global
internal record Options
{
    [CommandLine.Option(
        's', 
        "size", 
        Required = true,
        HelpText = "Size of the generated file in bytes. Size may slightly differ upward from the set value to allow all generated lines be written")]
    public long Size { get; set; }

    [CommandLine.Option('o', "output", Required = true, HelpText = "Path to the generated file")]
    public string Output { get; set; } = null!;
    
    [CommandLine.Option(
        'l',
        "string-length",
        Required = false,
        Default = 1024,
        HelpText = "Maximal string length of generated records. Max value is 2048 characters")]
    public int StringLength { get; set; }
    
    [CommandLine.Option(
        'c',
        "concurrency", 
        Required = false,
        Default = -1, 
        HelpText = "Degree of parallelism used to generate test data. If not set, system settings will be used")]
    public int Concurrency { get; set; }
    
    [CommandLine.Option(
        'd',
        "duplicates-ratio", 
        Required = false, 
        Default = 0.01, 
        HelpText = "Probability that random data will be taken from pre-generated array to provide duplicates")]
    public double DuplicatesRatio { get; set; }
}