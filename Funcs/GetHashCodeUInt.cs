// using System.Runtime.CompilerServices;

// namespace Alga.search;
// public static partial class Funcs
// {
//     [MethodImpl(MethodImplOptions.AggressiveInlining)]
//     public static uint GetHashCodeUInt(ReadOnlySpan<char> word)
//     {
//         uint hash = 0;
//         int len = Math.Min(4, word.Length);

//         for (int i = 0; i < len; i++)
//         {
//             hash |= (uint)char.ToLowerInvariant(word[i]) << (24 - (8 * i));
//         }

//         return hash;
//     }
// }