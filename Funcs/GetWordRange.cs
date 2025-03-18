using System.Runtime.CompilerServices;

namespace Alga.search;

internal static partial class Funcs {
    /// <summary>
    /// Determines the range of character values in a given word (the minimum and maximum character values).
    /// </summary>
    /// <param name="word">The word represented as a ReadOnlySpan<char> to find the character range.</param>
    /// <returns>
    /// A tuple containing the minimum and maximum character values in the word. 
    /// Returns null if the word is empty.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static (int Start, int End)? GetWordRange(ReadOnlySpan<char> word) {
        int len = word.Length;
        if (len == 0) return null;

        int min = word[0], max = word[0];

        for (int i = 1; i < len; i++) {
            int c = word[i];
            min = c < min ? c : min;
            max = c > max ? c : max;
        }

        return (min, max);
    }
}