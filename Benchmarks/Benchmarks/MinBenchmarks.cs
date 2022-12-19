using System.Text;
using BenchmarkDotNet.Attributes;
using ExternalSort;

namespace Benchmarks;

[MemoryDiagnoser()]
public class MinBenchmarks
{
    private readonly IComparer<FileRecord> _comparer = new FileRecordComparer(); 
    private readonly Random _random = new Random(100500 + 42);
    private FileRecord[] _unsorted = null!;
    
    private const string AllowedChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789 ";
    
    [GlobalSetup]
    public void GenerateStrings()
    {
        _unsorted = new FileRecord[ArrayLength];
        for (var i = 0; i < _unsorted.Length; i++)
        {
            _unsorted[i] = new FileRecord(_random.NextInt64(), new DisposableArraySegment<byte>(GenerateRandomString()));
        }
        
    }
    private byte[] GenerateRandomString()
    {
        var chars = Enumerable.Range(0, StringLength).Select(_ => (byte)AllowedChars[_random.Next(0, AllowedChars.Length)]).ToArray();
        return chars;
    }
    
    [Params(1000)] public int StringLength { get; set; } = 1024;
    
    [Params(10)] public int ArrayLength { get; set; }

    [Benchmark]
    public FileRecord? Linq_MinBy()
    {
        return _unsorted.MinBy(x => x, _comparer);
    }
    [Benchmark]
    public FileRecord? Sort_TakeFirst()
    {
        Array.Sort(_unsorted, _comparer);
        return _unsorted[0];
    }
}