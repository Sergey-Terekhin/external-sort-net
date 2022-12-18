namespace ExternalSort;

public static class LongHelper
{
    private static readonly long[] Digits =
    {
        1L,
        10L,
        100L,
        1000L,
        10000L,
        100000L,
        1000000L,
        10000000L,
        100000000L,
        1000000000L,
        10000000000L,
        100000000000L,
        1000000000000L,
        10000000000000L,
        100000000000000L,
        1000000000000000L,
        10000000000000000L,
        100000000000000000L,
        1000000000000000000L
    };

    public static int DigitsCount(this long value)
    {
        return value == long.MinValue ? 19 :
            value < 0L ? DigitsCount(-value) :
            value < Digits[8] ? // 1-8
            value < Digits[4] ? // 1-4
            value < Digits[2] ? // 1-2
            value < Digits[1] ? 1 : 2 : // 1-2
            value < Digits[3] ? 3 : 4 : // 3-4
            value < Digits[6] ? // 5-8
            value < Digits[5] ? 5 : 6 : // 5-6
            value < Digits[7] ? 7 : 8 : // 7-8
            value < Digits[16] ? // 9-16
            value < Digits[12] ? // 9-12
            value < Digits[10] ? // 9-10
            value < Digits[9] ? 9 : 10 : // 9-10
            value < Digits[11] ? 11 : 12 : // 11-12
            value < Digits[14] ? // 13-16
            value < Digits[13] ? 13 : 14 : // 13-14
            value < Digits[15] ? 15 : 16 : // 15-16
            value < Digits[17] ? 17 : // 17-19
            value < Digits[18] ? 18 :
            19;
    }
}