using System.Text;
using ExternalSort;
using FluentAssertions;
using NUnit.Framework;

namespace ExternalSortTests;

[TestFixture(TestOf = typeof(FileRecordComparer))]
public class FileRecordComparerTests
{
    [Test]
    [TestCase(1, "a", 1, "a", 0)]
    [TestCase(1, "a", 1, "A", 1)]
    [TestCase(1, "A", 1, "a", -1)]
    [TestCase(1, "a", 2, "a", -1)]
    [TestCase(2, "a", 1, "a", 1)]
    [TestCase(1, "a", 1, "aaaaa", 1)]
    [TestCase(1, "aaaaa", 1, "a", -1)]
    public void Compare(long leftNumber, string leftString, long rightNumber, string rightString, int expected)
    {
        var comparer = new FileRecordComparer();
        var leftRecord = new FileRecord(leftNumber, new(Encoding.ASCII.GetBytes(leftString)));
        var rightRecord = new FileRecord(rightNumber, new(Encoding.ASCII.GetBytes(rightString)));

        var result = comparer.Compare(leftRecord, rightRecord);
        switch (expected)
        {
            case 0:
                result.Should().Be(0);
                break;
            case > 0:
                result.Should().BePositive();
                break;
            case < 0:
                result.Should().BeNegative();
                break;
        }
    }
}