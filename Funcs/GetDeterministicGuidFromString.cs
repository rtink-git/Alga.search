using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Alga.search;

internal static partial class Funcs
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Guid GetDeterministicGuidFromString(ReadOnlySpan<char> span)
    {
        Span<byte> bytes = stackalloc byte[16];

        unchecked
        {
            for (int i = 0; i < span.Length; i++)
            {
                char c = span[i];
                // XOR младшего и старшего байта символа
                bytes[i % 16] = (byte)((bytes[i % 16] << 5) + bytes[i % 16] + (byte)(c ^ (c >> 8)));
            }
        }

        return MemoryMarshal.Read<Guid>(bytes);
    }
}