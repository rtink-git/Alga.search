using System;

namespace Alga.search;

public static partial class Funcs {
    public static double GetMatchCoefficient(double sameLength, double toLength, double tobeLength) => sameLength*2 / (toLength + tobeLength);
}
