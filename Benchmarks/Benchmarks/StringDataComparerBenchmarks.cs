using System.Text;
using BenchmarkDotNet.Attributes;
using ExternalSort;

namespace Benchmarks;

[MemoryDiagnoser()]
public class StringDataComparerBenchmarks
{
    private readonly Random _random = new Random(100500 + 42);
    private string[] _unsorted = null!;
    private ReadOnlyMemory<byte>[] _unsortedBytes = null!;
    
    private const string AllowedChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789 ";
    
    [GlobalSetup]
    public void GenerateStrings()
    {
        _unsorted = new string[ArrayLength];
        _unsortedBytes = new ReadOnlyMemory<byte>[ArrayLength];
        for (var i = 0; i < _unsorted.Length; i++)
        {
            _unsortedBytes[i] = GenerateRandomString();
            _unsorted[i] = Encoding.ASCII.GetString(_unsortedBytes[i].Span);
        }
        
    }
    private byte[] GenerateRandomString()
    {
        var chars = Enumerable.Range(0, StringLength).Select(_ => (byte)AllowedChars[_random.Next(0, AllowedChars.Length)]).ToArray();
        return chars;
    }
    
    [Params(100, 1000)] public int StringLength { get; set; } = 1024;
    
    [Params(1000)] public int ArrayLength { get; set; }

    [Benchmark]
    public void StringData_StringComparer_Ordinal()
    {
        Array.Sort(_unsorted, StringComparer.Ordinal);
    }

    [Benchmark]
    public void ByteData_AsciiDataComparer()
    {
        Array.Sort(_unsortedBytes, new AsciiDataComparer());
    }

    [Benchmark]
    public void ByteData_ConvertToString_And_Compare()
    {
        var unsorted = new string[_unsortedBytes.Length];
        for (var i = 0; i < _unsortedBytes.Length; i++)
        {
            unsorted[i] = Encoding.ASCII.GetString(_unsortedBytes[i].Span);
        }
        Array.Sort(unsorted, StringComparer.Ordinal);
    }
}