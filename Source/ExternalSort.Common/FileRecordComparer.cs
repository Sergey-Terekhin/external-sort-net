namespace ExternalSort;

public class FileRecordComparer : IComparer<FileRecord?>
{
    public static FileRecordComparer Default { get; } = new();
    private readonly AsciiDataComparer _dataComparer = new();
    public int Compare(FileRecord? x, FileRecord? y)
    {
        if (ReferenceEquals(x, y)) return 0;
        if (ReferenceEquals(null, y)) return 1;
        if (ReferenceEquals(null, x)) return -1;
            
        var dataComparison = _dataComparer.Compare(x.StringData.AsMemory(), y.StringData.AsMemory());
        return dataComparison != 0 ? dataComparison : x.Number.CompareTo(y.Number);
    }
}