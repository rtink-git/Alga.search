using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace Alga.search;
/// <summary>
/// Provides functionality for managing words and their associated data, including similarity calculations
/// </summary>
static class _Words {
    /// <summary>
    /// Represents the basic information about a word, including its hash code and character range.
    /// </summary>
    readonly struct WordInfo {
        public readonly long HashCode;
        public readonly int StartRange;
        public readonly int EndRange;

        /// <summary>
        /// Initializes a new instance of the <see cref="WordInfo"/> struct.
        /// </summary>
        /// <param name="hash">The hash code of the word</param>
        /// <param name="start">The starting character range of the word</param>
        /// <param name="end">The ending character range of the word</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public WordInfo(long hash, int start, int end) {
            HashCode = hash;
            StartRange = start;
            EndRange = end;
        }
    }

    /// <summary>
    /// A dictionary storing basic information about words, keyed by the word itself.
    /// </summary>
    /// <remarks>
    /// The key is the word, and the value is the corresponding <see cref="WordInfo"/>.
    /// The average size of each row is approximately 84 bytes.
    /// </remaarks>
    static readonly ConcurrentDictionary<string, WordInfo> BaseList = new();

    /// <summary>
    /// A dictionary of words' similarities, indexed by the word's hash code.A dictionary of words' similarities, indexed by the word's hash code.
    /// </summary>
    /// <remarks>
    /// The key is the word's hash code, and the value is a dictionary of similar words' hash codes and their similarity coefficients.
    /// The average size of each row is approximately 176 bytes.
    /// </remarks>
    internal static ConcurrentDictionary<long, ConcurrentDictionary<long, float>> SimilarsList { get; } = new();

    /// <summary>
    /// Tries to add a word to the <see cref="BaseList"/> and compute its similarity coefficients.
    /// </summary>
    /// <param name="HashCode">The hash code of the word</param>
    /// <param name="Line">The word string to be added</param>
    /// <returns>Returns <c>true</c> if the word was added successfully; otherwise, <c>false</c>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryAdd(long HashCode, string Line) {
        // var dt = DateTime.UtcNow; // for testing

        // try {
            if (string.IsNullOrWhiteSpace(Line) || BaseList.ContainsKey(Line)) return false;


            var wordRange = Funcs.GetWordRange(Line);
            if (wordRange is null) return false;

            var valueModel = new WordInfo(HashCode, wordRange.Value.Start, wordRange.Value.End);

            if (!BaseList.TryAdd(Line, valueModel)) return false;

            var siml = GetMatchCoefficientList(Line, valueModel.StartRange, valueModel.EndRange);

            if(siml?.Count > 0) {
                SimilarsList.TryAdd(HashCode, siml);

                foreach (var pair in siml) {
                    SimilarsList.AddOrUpdate(
                        pair.Key,
                        _ => new ConcurrentDictionary<long, float>(new[] { new KeyValuePair<long, float>(HashCode, pair.Value) }),
                        (_, existingDict) =>
                        {
                            existingDict.TryAdd(HashCode, pair.Value);
                            return existingDict;
                        }
                    );
                }
            }

            return true;
        // } catch {
        //     var testPoint = true;
        // } finally {
        //     var workTime = (DateTime.UtcNow - dt).TotalMicroseconds; // For testing
        //     var testPoint = true;
        // }

        // return false;
    }

    /// <summary>
    /// Computes the similarity coefficients for a given word and returns a dictionary of similar words' hash codes and their coefficients.
    /// </summary>
    /// <param name="hashCode">The hash code of the word for which similarities are being computed.</param>
    /// <param name="line">The word itself to be compared with other words.</param>
    /// <param name="startRange">The start range of the word's character values.</param>
    /// <param name="endRange">The end range of the word's character values.</param>
    /// <returns>
    /// A dictionary where the key is the hash code of the similar word, and the value is the similarity coefficient.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static ConcurrentDictionary<long, float>? GetMatchCoefficientList(string line, int startRange, int endRange)
    {
        var similarityDict = new ConcurrentDictionary<long, float>();

        var minCoefficient = line.Length switch { < 3 => 1, < 4 => 0.65, < 5 => 0.74f, < 6 => 0.65f, _ => 0.6f };

        BaseList.AsParallel().ForAll(word => {
            var wordInfo = word.Value; // Создаем копию структуры (избегаем ref)

            if (wordInfo.EndRange < startRange || wordInfo.StartRange > endRange) return;

            var matchString = Funcs.CompareStrings.GetMaximumMatchString(line, word.Key);
            if (matchString.IsEmpty) return;

            float coefficient = Funcs.CompareStrings.GetMatchCoefficient(matchString.Length, line.Length, word.Key.Length);
            if (coefficient < minCoefficient || coefficient >= 1) return;

            similarityDict.TryAdd(wordInfo.HashCode, coefficient);
        });

        return similarityDict;
    }
}