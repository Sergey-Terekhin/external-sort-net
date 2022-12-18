namespace ExternalSort;

// ReSharper disable once ClassNeverInstantiated.Global
internal record Options
{
    [CommandLine.Option('m', "memory-limit", Default = -1, Required = false, HelpText = "Memory limit in bytes to use. If set to -1, memory usage will be calculated automatically")]
    public long MemoryLimit { get; set; }

    [CommandLine.Option('i', "input", Required = true, HelpText = "Path to the unsorted file")]
    public string Input { get; set; } = null!;
    
    [CommandLine.Option('o', "output", Required = true, HelpText = "Path to the sorted file")]
    public string Output { get; set; } = null!;
    
    [CommandLine.Option('t',"temp", Default = null, Required = false, HelpText = "Folder to save temp data. If not set, system's temp folder will be used")]
    public string? Temp { get; set; }
    
    [CommandLine.Option('b',"block-size", Default = -1, HelpText = "Size of the block for external merge. If not set, size will be determined automatically based on available memory and input file size")]
    public long BlockSize { get; set; }

    public int BlockCount { get; set; }
    public List<string> BlockFiles { get; set; } = null!;
}