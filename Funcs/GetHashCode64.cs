using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Alga.search;
internal static partial class Funcs {
    /// <summary>
    /// Computes a 64-bit hash code for the provided input string using a custom hash function.
    /// </summary>
    /// <param name="value">The input string represented as a ReadOnlySpan</param>
    /// <returns>A 64-bit long value representing the hash code of the input string</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long GetHashCode64(ReadOnlySpan<char> span) {
        unchecked {
            long hash = 5381 + span.Length;
            ref char start = ref MemoryMarshal.GetReference(span);

            for (int i = 0; i < span.Length; i++)
                hash = (hash << 5) - hash + Unsafe.Add(ref start, i);

            return hash;
        }
    }
}

    //[MethodImpl(MethodImplOptions.AggressiveInlining)]
    // public static long GetHashCode64(ReadOnlySpan<char> span) {
    //     unchecked {
    //         long hash = 17;
    //         foreach (char c in span)
    //             hash = hash * 31 + c;
    //         return hash;
    //     }
    // }