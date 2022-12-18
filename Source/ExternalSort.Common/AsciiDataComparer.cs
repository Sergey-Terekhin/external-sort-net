namespace ExternalSort;

/// <summary>
/// Comparer for ascii strings represented as byte arrays
/// </summary>
/// <remarks> it's port of <see cref="StringComparer.Ordinal"/> for byte arrays </remarks>
public class AsciiDataComparer : IComparer<ReadOnlyMemory<byte>>
{
    public int Compare(ReadOnlyMemory<byte> x, ReadOnlyMemory<byte> y)
    {
        if (x.Length == 0 && y.Length == 0) return 0;
        if (x.Length == 0 && y.Length != 0) return 1;
        if (x.Length != 0 && y.Length == 0) return -1;

        return CompareOrdinalHelper(x, y);
    }

    private static int CompareOrdinalHelper(ReadOnlyMemory<byte> x, ReadOnlyMemory<byte> y)
    {
        var length = Math.Min(x.Length, y.Length);
        var xSpan = x.Span;
        var ySpan = y.Span;
        for (var i = 0; i < length; i++)
        {
            if (xSpan[i] != ySpan[i])
            {
                return xSpan[i] - ySpan[i];
            }
        }

        // At this point, we have compared all the characters in at least one string.
        // The longer string will be larger.
        return y.Length - x.Length;
    }
}