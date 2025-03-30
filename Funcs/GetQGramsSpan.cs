using System.Runtime.CompilerServices;
using System.Collections.Frozen;
using System.Runtime.InteropServices;


namespace Alga.search;
internal static partial class Funcs {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static FrozenSet<int> GetQGramHashes(ReadOnlySpan<char> span, int q) {
        int count = span.Length - q + 1;
        if (count <= 0) return FrozenSet<int>.Empty;

        // Используем stackalloc для небольших массивов, иначе - HashSet<int>
        Span<int> buffer = count <= 128 ? stackalloc int[count] : new int[count];
        ref char start = ref MemoryMarshal.GetReference(span); // Ускоряем доступ

        for (int i = 0; i < count; i++) {
            unchecked {
                int hash = 5381; // Улучшенный seed
                for (int j = 0; j < q; j++)
                    hash = (hash << 5) - hash + Unsafe.Add(ref start, i + j);

                buffer[i] = hash;
            }
        }

        return buffer.ToArray().ToFrozenSet();
    }
}

    // [MethodImpl(MethodImplOptions.AggressiveInlining)]
    // public static FrozenSet<int> GetQGramHashes(ReadOnlySpan<char> span, int q) {
    //     HashSet<int> hashSet = new HashSet<int>(span.Length - q + 1);

    //     for (int i = 0; i <= span.Length - q; i++)
    //         unchecked {
    //             int hash = 17;
    //             for (int j = 0; j < q; j++)
    //                 hash = hash * 31 + span[i + j];

    //             hashSet.Add(hash);
    //         }

    //     return hashSet.ToFrozenSet();
    // }