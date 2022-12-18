using System.Text;

namespace ExternalSort;

/// <summary>
/// Extension methods for <see cref="FileRecord"/>
/// </summary>
public static class FileRecordExtensions
{
    public static int ByteLength(this FileRecord record) =>
        record.Number.DigitsCount() + record.StringData.Count + Constants.SeparatorLength + Constants.SeparatorLength;

    public static int WriteTo(this FileRecord record, Span<byte> buffer)
    {
        var writtenLength = 0;
        var numberSpan = buffer[writtenLength..];
        writtenLength += Encoding.ASCII.GetBytes(record.Number.ToString(), numberSpan);

        var separatorSpan = buffer.Slice(writtenLength, Constants.SeparatorLength);
        Constants.Separator.CopyTo(separatorSpan);
        writtenLength += Constants.SeparatorLength;

        var stringSpan = buffer[writtenLength..];
        record.StringData.AsSpan().CopyTo(stringSpan);
        writtenLength += record.StringData.Count;

        var eolSpan = buffer.Slice(writtenLength, Constants.EndOfLineLength);
        Constants.EndOfLine.CopyTo(eolSpan);
        writtenLength += Constants.EndOfLineLength;
        return writtenLength;
    }
}