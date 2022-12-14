using BenchmarkDotNet.Attributes;

namespace Benchmarks;

[MemoryDiagnoser]
public class SortingAlgorithmBenchmarks
{
    private readonly Random _random = new Random(100500 + 42);
    private string[] _unsorted = null!;
    
    private const string AllowedChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789 ";

    public int StringLength { get; set; } = 1024;
    
    [Params(1000, 10000)] public int ArrayLength { get; set; }

    [GlobalSetup]
    public void GenerateStrings()
    {
        _unsorted = new string[ArrayLength];
        for (var i = 0; i < _unsorted.Length; i++)
        {
            _unsorted[i] = GenerateRandomString();
        }
        
    }

    [Benchmark]
    public string[] ArraySort_OrdinalCompare()
    {
        Array.Sort(_unsorted);
        return _unsorted;
    }

    [Benchmark]
    public string[] MultikeyQuickSort()
    {
        return Benchmarks.MultikeyQuickSort.InPlaceSort(_unsorted);
    }
    private string GenerateRandomString()
    {
        var chars = Enumerable.Range(0, StringLength).Select(_ => AllowedChars[_random.Next(0, AllowedChars.Length)]).ToArray();
        return new string(chars);
    }
}