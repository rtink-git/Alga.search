using System.Runtime.CompilerServices;

namespace Alga.search;

public static partial class Funcs
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long CreateUniqueId(byte listId, long id)
    {
        return ((long)listId << 56) | (id & 0x00FFFFFFFFFFFFFF);
    }
}
