using System.Runtime.CompilerServices;

namespace Alga.search;
internal static partial class Funcs {
    public static class CompareStrings {
        /// <summary>
        /// Compare two strings, looks for a match not only from the beginning of the string
        /// </summary>
        /// <param name="spanOne"></param>
        /// <param name="spanTwo"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ReadOnlySpan<char> GetMaximumMatchString(ReadOnlySpan<char> spanOne, ReadOnlySpan<char> spanTwo) {
            int len1 = spanOne.Length, len2 = spanTwo.Length;
            if (len1 == 0 || len2 == 0) return ReadOnlySpan<char>.Empty;

            int[] dp = new int[len2 + 1]; 
            int maxLength = 0, endIndex = 0;

            for (int i = 1; i <= len1; i++) {
                int prev = 0;
                for (int j = 1; j <= len2; j++) {
                    int temp = dp[j];
                    if (spanOne[i - 1] == spanTwo[j - 1]) {
                        dp[j] = prev + 1;
                        if (dp[j] > maxLength) {
                            maxLength = dp[j];
                            endIndex = i;
                        }
                    } else
                        dp[j] = 0;
                    prev = temp;
                }
            }

            return maxLength > 0 ? spanOne.Slice(endIndex - maxLength, maxLength) : ReadOnlySpan<char>.Empty;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float GetMatchCoefficient(float sameLength, float toLength, float tobeLength) => 2 * sameLength / (toLength + tobeLength);
    }
}