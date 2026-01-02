using System.Runtime.CompilerServices;

namespace Alga.search;

public static partial class Models
{
    /// <summary>
    /// Represents the basic information about a word, including its hash code and character range.
    /// </summary>
    internal readonly struct WordInfo
    {
        public readonly int StartRange;
        public readonly int EndRange;
        public readonly ushort[] Qgrams;

        //public readonly FrozenSet<ushort> Qgrams;

        /// <summary>
        /// Initializes a new instance of the <see cref="WordInfo"/> struct.
        /// </summary>
        /// <param name="start">The starting character range of the word</param>
        /// <param name="end">The ending character range of the word</param>
        /// <param name="qgrams">The frozen set of q-grams</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public WordInfo(int start, int end, ushort[] qgrams)
        {
            StartRange = start;
            EndRange = end;
            Qgrams = qgrams;
        }
    }
}
