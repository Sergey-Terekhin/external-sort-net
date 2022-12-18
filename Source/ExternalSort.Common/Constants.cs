using System.Text;

namespace ExternalSort;

public static class Constants
{
    public const int MaxLongDigits = 19; //digits count in the long.MaxValue value (9,223,372,036,854,775,807)
    public const int MaxStringLength = 2048;

    public const string AllowedChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789 ";
    public const string AllowedFirstChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
    public static readonly byte[] Separator = Encoding.ASCII.GetBytes(". ");
    public static readonly byte[] EndOfLine = Encoding.ASCII.GetBytes("\r\n");
    public static readonly int SeparatorLength = Separator.Length;
    public static readonly int EndOfLineLength = EndOfLine.Length;
    public static readonly int MaxLineLength = MaxLongDigits + MaxStringLength + SeparatorLength + EndOfLineLength;
}