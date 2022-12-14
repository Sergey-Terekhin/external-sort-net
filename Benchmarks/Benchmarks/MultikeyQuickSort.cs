namespace Benchmarks;

// taken from https://www.codeproject.com/Articles/146086/Fast-String-Sort-in-C-and-F
// algorithm description here: https://en.wikipedia.org/wiki/Multi-key_quicksort
//
// Benchmark results: 
// BenchmarkDotNet=v0.13.2, OS=Windows 10 (10.0.19044.1766/21H2/November2021Update)
// Intel Core i7-10850H CPU 2.70GHz, 1 CPU, 12 logical and 6 physical cores
// .NET SDK=6.0.100
//   [Host]     : .NET 6.0.0 (6.0.21.52210), X64 RyuJIT AVX2
//   DefaultJob : .NET 6.0.0 (6.0.21.52210), X64 RyuJIT AVX2
// 
// 
// |                   Method | ArrayLength |        Mean |      Error |     StdDev | Allocated |
// |------------------------- |------------ |------------:|-----------:|-----------:|----------:|
// | ArraySort_OrdinalCompare |        1000 |   480.11 us |   5.503 us |   5.148 us |         - |
// |        MultikeyQuickSort |        1000 |    51.14 us |   0.317 us |   0.296 us |         - |
// | ArraySort_OrdinalCompare |       10000 | 8,902.62 us | 132.200 us | 117.192 us |       8 B |
// |        MultikeyQuickSort |       10000 |   923.96 us |   6.032 us |   5.642 us |       1 B |

public static class MultikeyQuickSort
{
    public static string[] Sort(string[] input)
    {
        string[] copy = new string[input.Length];
        input.CopyTo(copy, 0);
        return InPlaceSort(copy);
    }

    public static string[] InPlaceSort(string[] input)
    {
        InPlaceSort(input, 0, 0, input.Length);
        return input;
    }

    static void insertionSort(string[] x, int a, int len, int depth)
    {
        int pi = a + 1;
        int n = len - 1;
        //propages maximum to last position
        while (n > 0)
        {
            int pj = pi;
            while (pj > a)
            {
                int d = depth;
                int pj1 = pj - 1;
                string s = x[pj1];
                string t = x[pj];
                int s_len = s.Length;
                int t_len = t.Length;
                int min_len = s_len;
                if (t_len < s_len) min_len = t_len;
                while (d < min_len && s[d] == t[d])
                {
                    d = d + 1;
                }
                if (d == s_len || (d < t_len && s[d] <= t[d]))
                {
                    break;
                }
                else
                {
                    string tmp = x[pj];
                    x[pj] = x[pj1];
                    x[pj1] = tmp;
                    pj = pj1;
                }
            }
            n = n - 1;
            pi = pi + 1;
        }
    }

    static void swap(string[] arr, int i, int j)
    {
        string tmp = arr[i];
        arr[i] = arr[j];
        arr[j] = tmp;
    }

    static int fullCmp(int depth, string a, string b)
    {
        int lenA = a.Length;
        int lenB = b.Length;
        if (lenA == depth && lenB == depth)
            return 0;
        else
        if (lenA == depth)
            return -1; //a is empty, b is not empty
        else if (lenB == depth)
            return 1;
        else
        {
            char ch1 = a[depth];
            char ch2 = b[depth];
            if (ch1 < ch2)
            {
                return -1;
            }
            else if (ch1 == ch2)
                return 0;
            else
                return 1;
        }
    }

    static int cmpByPivotChar(int depth, string a, char ch2)
    {
        int lenA = a.Length;
        if (lenA == depth)
            return -1;
        else
        {
            char ch1 = a[depth];
            if (ch1 < ch2)
                return (-1);
            else if (ch1 == ch2)
                return 0;
            else
                return 1;
        }
    }

    static int cmpByPivotLen(int depth, string a)
    {
        int lenA = a.Length;
        if (lenA == depth)
            return 0;
        else
            return 1;
    }

    static int midOf(int a, int b)
    {
        return (a + ((b - a + 1) >> 1));
    }

    static void swapIfGreater(string[] input, int depth, int i, int j)
    {
        if (i != j && (fullCmp(depth, input[i], input[j]) > 0))
        {
            swap(input, i, j);
        }
    }

    static void sortPivots(string[] input, int depth, int s, int m, int e)
    {
        swapIfGreater(input, depth, s, m); //st1 <= mid
        swapIfGreater(input, depth, s, e); //st1 <= ed1
        swapIfGreater(input, depth, m, e); //mid <= ed1
    }

    static char at(string s, int pos)
    {
        if (pos >= s.Length)
            return (char)0;
        return s[pos];
    }

    static int med3func(string[] x, int a, int b, int c, int depth)
    {
        char va, vb, vc;
        if ((va = at(x[a], depth)) == (vb = at(x[b], depth)))
            return a;
        if ((vc = at(x[c], depth)) == va || vc == vb)
            return c;
        return va < vb ?
            (vb < vc ? b : (va < vc ? c : a))
            : (vb > vc ? b : (va < vc ? a : c));
    }

    static void InPlaceSort(string[] input, int depth, int st, int ed)
    {
        int len = ed - st;
        if (len > 1)
        {
            if (len < 10)
            {
                //this in general depends on the length of the strings to be sorted
                //if the strings are long make the bound less (e.g. 10)
                insertionSort(input, st, len, depth);
            }
            else
            {
                int st1 = st;
                int ed1 = ed - 1;
                int eqStart;
                int eqEnd;


                //int mid = midOf(st1, ed1);

                //if (len > 30)
                //{
                //    int mid1 = midOf(st1, mid);
                //    int mid2 = midOf(mid, ed1);
                //    sortPivots(input, depth, st1, mid1, mid);
                //    sortPivots(input, depth, mid, mid2, ed1);
                //    sortPivots(input, depth, mid1, mid, mid2);
                //}
                //else
                //{
                //    sortPivots(input, depth, st1, mid, ed1);
                //}
                //string pivot = input[mid];

                int pl = st;
                int pm = st + (len / 2);
                int pn = ed1;
                if (len > 30)
                { // On big arrays, pseudomedian of 9
                    int d = (len / 8);
                    pl = med3func(input, pl, pl + d, pl + 2 * d, depth);
                    pm = med3func(input, pm - d, pm, pm + d, depth);
                    pn = med3func(input, pn - 2 * d, pn - d, pn, depth);
                }
                pm = med3func(input, pl, pm, pn, depth);
                string pivot = input[pm];

                if (depth < pivot.Length)
                {
                    char pivotChar = pivot[depth];
                    while (st1 <= ed1 && 0 == (cmpByPivotChar(depth, input[st1], pivotChar)))
                    {
                        st1 = st1 + 1;
                    }

                    eqStart = st1 - 1;

                    while (st1 <= ed1 && 0 == (cmpByPivotChar(depth, input[ed1], pivotChar)))
                    {
                        ed1 = ed1 - 1;
                    }

                    eqEnd = ed1 + 1;

                    int cmpFlag = 0;
                    bool flag = (st1 <= ed1);

                    while (flag)
                    {
                        while (flag)
                        {
                            cmpFlag = cmpByPivotChar(depth, input[st1], pivotChar);
                            if (cmpFlag < 0)
                            {
                                st1 = st1 + 1;
                                flag = (st1 <= ed1);
                            }
                            else if (cmpFlag == 0)
                            {
                                eqStart = eqStart + 1;
                                swap(input, st1, eqStart);
                                st1 = st1 + 1;
                            }
                            else
                                flag = false;
                        }


                        flag = (st1 <= ed1);
                        while (flag)
                        {
                            cmpFlag = cmpByPivotChar(depth, input[ed1], pivotChar);
                            if (cmpFlag > 0)
                            {
                                ed1 = ed1 - 1;
                                flag = (st1 <= ed1);
                            }
                            else if (cmpFlag == 0)
                            {
                                eqEnd = eqEnd - 1;
                                swap(input, ed1, eqEnd);
                                ed1 = ed1 - 1;
                            }
                            else
                                flag = false;
                        }
                        flag = (st1 <= ed1);
                        if (flag)
                        {
                            swap(input, st1, ed1);
                            st1 = st1 + 1;
                            ed1 = ed1 - 1;
                        }
                    }
                }
                else
                {
                    while (st1 <= ed1 && 0 == (cmpByPivotLen(depth, input[st1])))
                    {
                        st1 = st1 + 1;
                    }
                    eqStart = st1 - 1;

                    while (st1 <= ed1 && 0 == (cmpByPivotLen(depth, input[ed1])))
                    {
                        ed1 = ed1 - 1;
                    }
                    eqEnd = ed1 + 1;

                    int cmpFlag = 0;
                    bool flag = (st1 <= ed1);

                    while (flag)
                    {
                        while (flag)
                        {
                            cmpFlag = cmpByPivotLen(depth, input[st1]);
                            if (cmpFlag < 0)
                            {
                                st1 = st1 + 1;
                                flag = (st1 <= ed1);
                            }
                            else if (cmpFlag == 0)
                            {
                                eqStart = eqStart + 1;
                                swap(input, st1, eqStart);
                                st1 = st1 + 1;
                            }
                            else
                                flag = false;
                        }
                        flag = (st1 <= ed1);
                        while (flag)
                        {
                            cmpFlag = cmpByPivotLen(depth, input[ed1]);
                            if (cmpFlag > 0)
                            {
                                ed1 = ed1 - 1;
                                flag = (st1 <= ed1);
                            }
                            else if (cmpFlag == 0)
                            {
                                eqEnd = eqEnd - 1;
                                swap(input, ed1, eqEnd);
                                ed1 = ed1 - 1;
                            }
                            else
                                flag = false;
                        }
                        flag = (st1 <= ed1);
                        if (flag)
                        {
                            swap(input, st1, ed1);
                            st1 = st1 + 1;
                            ed1 = ed1 - 1;
                        }
                    }
                }
                int i = st;
                int j = st1 - 1;
                while (i <= eqStart)
                {
                    swap(input, i, j);
                    i = i + 1;
                    j = j - 1;
                }

                i = ed - 1;
                j = st1;
                while (i >= eqEnd)
                {
                    swap(input, i, j);
                    i = i - 1;
                    j = j + 1;
                }

                eqEnd = st1 + (ed - eqEnd);

                if (ed - eqEnd > 1)
                {
                    InPlaceSort(input, depth, eqEnd, ed);
                }
                eqStart = st1 - (eqStart + 1 - st);
                //st..eqStart..eqEnd..ed
                if (eqEnd - eqStart > 1)
                {
                    if (input[eqStart].Length > depth)
                    {
                        InPlaceSort(input, (depth + 1), eqStart, eqEnd);
                    }
                }
                if (eqStart - st > 1)
                {
                    InPlaceSort(input, depth, st, eqStart);
                }
            }
        }

    }
}