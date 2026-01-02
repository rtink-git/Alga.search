using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace Alga.search;
/// <summary>
/// Provides functionality for managing words and their associated data, including similarity calculations
/// </summary>
static class Words
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryAdd(long HashCode, string Line, Dictionary<long, Models.WordInfo> wordsMap, Dictionary<long, ConcurrentDictionary<long, float>> wordSimilarityMap)
    {
        if (wordsMap.ContainsKey(HashCode)) return false;

        var wordRange = Funcs.GetWordRange(Line);
        if (wordRange is null) return false;

        var qGmams = Funcs.GetQGramHashes(Line, 2);

        var valueModel = new Models.WordInfo(wordRange.Value.Start, wordRange.Value.End, qGmams);

        if (!wordsMap.TryAdd(HashCode, valueModel)) return false;

        if (Line.Length > 2)
        {
            var siml = GetMatchCoefficientList(Line, valueModel, wordsMap);

            if (siml?.Count > 0)
            {
                wordSimilarityMap.TryAdd(HashCode, siml); // поменять

                foreach (var pair in siml)
                {
                    if (!wordSimilarityMap.TryGetValue(pair.Key, out var existingDict)) wordSimilarityMap[pair.Key] = new ConcurrentDictionary<long, float>(new[] { new KeyValuePair<long, float>(HashCode, pair.Value) });
                    else existingDict.TryAdd(HashCode, pair.Value);
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
    static ConcurrentDictionary<long, float> GetMatchCoefficientList(ReadOnlySpan<char> line, Models.WordInfo wInfo, Dictionary<long, Models.WordInfo> wordsMap)
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

        Parallel.ForEach(wordsMap, options, word =>
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