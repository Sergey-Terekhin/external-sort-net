namespace ExternalSort;

public class FileRecordComparer : IComparer<FileRecord?>
{
    public static FileRecordComparer Default { get; } = new();
    private readonly StringComparer _dataComparer = StringComparer.Ordinal;
    public int Compare(FileRecord? x, FileRecord? y)
    {
        if (ReferenceEquals(x, y)) return 0;
        if (ReferenceEquals(null, y)) return 1;
        if (ReferenceEquals(null, x)) return -1;
            
        var dataComparison = _dataComparer.Compare(x.StringData, y.StringData);
        return dataComparison != 0 ? dataComparison : x.Number.CompareTo(y.Number);
    }
}