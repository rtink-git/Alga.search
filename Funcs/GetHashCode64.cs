using System.Runtime.CompilerServices;

namespace Alga.search;

internal static partial class Funcs {
    /// <summary>
    /// Computes a 64-bit hash code for the provided input string using a custom hash function.
    /// </summary>
    /// <param name="value">The input string represented as a ReadOnlySpan</param>
    /// <returns>A 64-bit long value representing the hash code of the input string</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long GetHashCode64(ReadOnlySpan<char> value) {
        long hash = 525201411107845655L; // Seed
        const long prime = 0x517CC1B727220A95L; // Large prime multiplier

        for (int i = 0; i < value.Length; i++) {
            hash ^= value[i];
            hash *= prime;
        }

        return hash;
    }
}