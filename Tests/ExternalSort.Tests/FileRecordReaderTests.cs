using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ExternalSort;
using FluentAssertions;
using FluentAssertions.Equivalency;
using NUnit.Framework;

namespace ExternalSortTests;

[TestFixture(TestOf = typeof(FileRecordReader))]
public class Tests
{
    [SetUp]
    public void Setup()
    {
    }

    [Test]
    [TestCase("1. first\r\n")]
    [TestCase("1. first\r\n22222. long continuation\r\n")]
    [TestCase("22222. long first part\r\n11. continuation\r\n")]
    public void ReadFrom(string data)
    {
        var expectedRecords = data.Split("\r\n", StringSplitOptions.RemoveEmptyEntries)
            .Select(s =>
            {
                var parts = s.Split(". ", StringSplitOptions.RemoveEmptyEntries);
                return new FileRecord(long.Parse(parts[0]), new(Encoding.ASCII.GetBytes(parts[1])));
            }).ToArray();
        using var reader = new FileRecordReader(Stream.Null, ExternalSortImpl.ReadBufferSize);
        var bytes = Encoding.ASCII.GetBytes(data);
        using var records = reader.ReadFrom(bytes, out _, out _);

        records.Value.Should().BeEquivalentTo(expectedRecords, opt => opt.Using(new FileRecordEqualityComparer()));
    }

    [Test]
    [TestCase("1. abcd\r\n22222")]
    [TestCase("1. abcd\r\n22222.")]
    [TestCase("1. abcd\r\n22222. continuation")]
    [TestCase("1. abcd\r\n22222. long continuation\r")]
    public void ReadFrom_IncompleteData(string data)
    {
        var expectedRecords = data.Split("\r\n", StringSplitOptions.RemoveEmptyEntries)
            .Take(1)
            .Select(s =>
            {
                var parts = s.Split(". ", StringSplitOptions.RemoveEmptyEntries);
                return new FileRecord(long.Parse(parts[0]), new(Encoding.ASCII.GetBytes(parts[1])));
            }).ToArray();
        using var reader = new FileRecordReader(Stream.Null, ExternalSortImpl.ReadBufferSize);
        var bytes = Encoding.ASCII.GetBytes(data);
        using var records = reader.ReadFrom(bytes, out _, out _);

        records.Value.Should().BeEquivalentTo(expectedRecords, opt => opt.Using(new FileRecordEqualityComparer()));
    }

    [Test]
    [TestCase("22222. long first part\r\n11. continuation\r\n", ExternalSortImpl.ReadBufferSize)]
    [TestCase("22222. long first part\r\n11. continuation\r\n", 30)]
    [TestCase("22222. long first part\r\n11. continuation\r\n14. continuation 2\r\n", 30)]
    public async Task ReadAsync(string data, int blockSize)
    {
        var expectedRecords = data.Split("\r\n", StringSplitOptions.RemoveEmptyEntries)
            .Select(s =>
            {
                var parts = s.Split(". ", StringSplitOptions.RemoveEmptyEntries);
                return new FileRecord(long.Parse(parts[0]), new(Encoding.ASCII.GetBytes(parts[1])));
            }).ToArray();
        var stream = new MemoryStream(Encoding.ASCII.GetBytes(data));
        await using var reader = new FileRecordReader(stream, blockSize);
        var result = new List<FileRecord>();
        while (true)
        {
            var records = await reader.ReadAsync(blockSize);
            if (records.Count == 0)
                break;

            result.AddRange(records);
        }

        result.Should().BeEquivalentTo(expectedRecords, opt => opt.Using(new FileRecordEqualityComparer()));
    }

    [Test]
    [TestCase(1024)]
    [TestCase(23831)]
    [TestCase(512 * 1024)]
    [TestCase(10 * 1024 * 1024)]
    public async Task ReadFromDirectoryAsync(int blockSize)
    {
        foreach (var file in Directory.EnumerateFiles(@"C:\Users\terekhin\Downloads\output_10mb_20_tmp"))
        {
            TestContext.WriteLine($"Reading from file {file}");
            var stream = File.OpenRead(file);
            await using var reader = new FileRecordReader(stream, blockSize);
            var result = new List<FileRecord>();
            while (true)
            {
                TestContext.WriteLine($"Already read {result.Count} records");
                var act = () => reader.ReadAsync(blockSize);
                var readResult = await act.Should().NotThrowAsync();
                var records = readResult.Subject;
                if (records.Count == 0)
                    break;

                result.AddRange(records);
            }

            result.Should().NotBeEmpty();
        }
    }
}

internal class FileRecordEqualityComparer : IEqualityComparer<FileRecord>
{
    private readonly FileRecordComparer _comparer = new();

    public bool Equals(FileRecord x, FileRecord y)
    {
        return _comparer.Compare(x, y) == 0;
    }

    public int GetHashCode(FileRecord obj)
    {
        return HashCode.Combine(obj.Number, obj.StringData);
    }
}