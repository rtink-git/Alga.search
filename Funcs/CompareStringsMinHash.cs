using System.Collections.Frozen;
using System.Runtime.CompilerServices;

namespace Alga.search;

public static partial class Funcs
{
    public static float CompareStringsMinHash(ushort[] a, ushort[] b)
    {
        int countA = a.Length;
        int countB = b.Length;

        if (countA == 0 || countB == 0) return 0f;

        int intersection = 0;

        if (countA <= countB)
        {
            foreach (var x in a)
            {
                if (ContainsQgramOptimized(b, x))
                    intersection++;
            }
        }
        else
        {
            foreach (var x in b)
            {
                if (ContainsQgramOptimized(a, x))
                    intersection++;
            }
        }

        return (float)intersection / (countA + countB - intersection);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool ContainsQgramOptimized(ushort[] arr, ushort q)
    {
        switch (arr.Length)
        {
            case 1: return arr[0] == q;
            case 2: return arr[0] == q || arr[1] == q;
            case 3: return arr[0] == q || arr[1] == q || arr[2] == q;
            case 4: return arr[0] == q || arr[1] == q || arr[2] == q || arr[3] == q;
            default: return Array.IndexOf(arr, q) >= 0; // Для массивов >4 элементов
        }
    }

    // [MethodImpl(MethodImplOptions.AggressiveInlining)]
    // public static float CompareStringsMinHash(ushort[] a, ushort[] b)
    // {
    //     int countA = a.Length;
    //     int countB = b.Length;

    //     if (countA == 0 || countB == 0) return 0f;

    //     int intersection = 0;

    //     if (countA <= countB)
    //     {
    //         foreach (var x in a)
    //             if (b.Contains(x))
    //                 intersection++;
    //     }
    //     else
    //     {
    //         foreach (var x in b)
    //             if (a.Contains(x))
    //                 intersection++;
    //     }

    //     return (float)intersection / (countA + countB - intersection);
    // }

    // [MethodImpl(MethodImplOptions.AggressiveInlining)]
    // public static float CompareStringsMinHash(FrozenSet<int> qGramOne, FrozenSet<int> qGramTwo) {
    //     if (qGramOne.Count > qGramTwo.Count) 
    //         (qGramOne, qGramTwo) = (qGramTwo, qGramOne);

    //     int intersection = 0;

    //     foreach (var q in qGramTwo)
    //         if (qGramOne.Contains(q))
    //             intersection++;

    //     return (float)intersection / (qGramOne.Count + qGramTwo.Count - intersection);
    // }
}
