using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace Alga.search;
/// <summary>
/// Provides functionality for managing words and their associated data, including similarity calculations
/// </summary>
static class Words
{
    /// <summary>
    /// Tries to add a word to the <see cref="BaseList"/> and compute its similarity coefficients.
    /// </summary>
    /// <param name="HashCode">The hash code of the word</param>
    /// <param name="Line">The word string to be added</param>
    /// <returns>Returns <c>true</c> if the word was added successfully; otherwise, <c>false</c>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryAdd(long HashCode, string Line)
    {
        if (Collections.WordsMap.ContainsKey(HashCode)) return false; //string.IsNullOrWhiteSpace(Line) || 

        var wordRange = Funcs.GetWordRange(Line);
        if (wordRange is null) return false;

        var qGmams = Funcs.GetQGramHashes(Line, 2);

        var valueModel = new Modules.WordInfo(wordRange.Value.Start, wordRange.Value.End, qGmams);

        if (!Collections.WordsMap.TryAdd(HashCode, valueModel)) return false;

        if (Line.Length > 2)
        {
            var siml = GetMatchCoefficientList(Line, valueModel);

            if (siml?.Count > 0)
            {
                Collections.WordSimilarityMap.TryAdd(HashCode, siml);

                foreach (var pair in siml)
                {
                    Collections.WordSimilarityMap.AddOrUpdate(
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
        }

        return true;
    }

    /// <summary>
    /// Calculates similarity coefficients between an input string and words from a preprocessed collection.
    /// Returns a dictionary of word hashes and their similarity scores that meet the minimum threshold.
    /// </summary>
    /// <param name="line">Input string to compare against the word collection</param>
    /// <param name="wInfo">Word metadata containing Q-grams and position range</param>
    static ConcurrentDictionary<long, float> GetMatchCoefficientList(ReadOnlySpan<char> line, Modules.WordInfo wInfo)
    {
        float minCoefficient = line.Length switch
        {
            < 3 => 1f,
            < 4 => 0.65f,
            < 5 => 0.6f,
            < 6 => 0.55f,
            _ => 0.5f
        };

        var resultDict = new ConcurrentDictionary<long, float>();
        var options = new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount };

        Parallel.ForEach(Collections.WordsMap, options, word =>
        {
            var value = word.Value;

            if (value.EndRange < wInfo.StartRange || value.StartRange > wInfo.EndRange)
                return;

            float coefficient = Funcs.CompareStringsMinHash(wInfo.Qgrams, value.Qgrams);

            if (coefficient >= minCoefficient && coefficient < 1f)
            {
                // TryAdd — не перезапишет, если ключ уже есть
                resultDict.TryAdd(word.Key, coefficient);
            }
        });

        return resultDict;
    }
}