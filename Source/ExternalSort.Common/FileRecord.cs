using System.Text;

namespace ExternalSort;

/// <summary>
/// File record used for generation or sorting
/// </summary>
/// <param name="Number">Number value</param>
/// <param name="StringData">String data. Because it's requirement to use only ASCII symbols, it's not necessary to parse strings</param>
public record FileRecord(long Number, string StringData) : IDisposable
//public record FileRecord(long Number, DisposableArraySegment<byte> StringData):IDisposable
{
    protected virtual bool PrintMembers(StringBuilder builder)
    {
        builder.Append(nameof(Number));
        builder.Append(": ");
        builder.Append(Number);

        builder.Append("; ");
        builder.Append(nameof(StringData));
        builder.Append(": ");
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        //if (StringData.Value.Array != null)
        if (StringData != null)
        {
            // var bytes = StringData.AsSpan();
            //builder.Append( Encoding.ASCII.GetString(bytes));
            builder.Append(StringData);
        }
        else
        {
            builder.Append("<null>");
        }

        return true;
    }

    public void Dispose()
    {
       // StringData.Dispose();
    }
}