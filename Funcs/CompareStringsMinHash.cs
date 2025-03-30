using System.Collections.Frozen;
using System.Runtime.CompilerServices;

namespace Alga.search;

internal static partial class Funcs {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float CompareStringsMinHash(FrozenSet<int> qGramOne, FrozenSet<int> qGramTwo) {
        if (qGramOne.Count > qGramTwo.Count) 
            (qGramOne, qGramTwo) = (qGramTwo, qGramOne);
            
        int intersection = 0;
        
        foreach (var q in qGramTwo)
            if (qGramOne.Contains(q))
                intersection++;
        
        return (float)intersection / (qGramOne.Count + qGramTwo.Count - intersection);
    }
}
