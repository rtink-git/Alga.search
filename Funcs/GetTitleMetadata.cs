using System.Collections.Frozen;
using System.Runtime.CompilerServices;
using System.Globalization;

namespace Alga.search;
internal static partial class Funcs {
    private static readonly char[] Separators = { ' ', ',', '.', '!', '?', ':', ';', '-', '_', '"', '\'', '/', '\\', '|', '\t', '\n' };

    /// <summary>
    /// Retrieves metadata for a given title by normalizing the title and extracting unique words with their hashes.
    /// The method splits the title into words, normalizes them, and returns a hash of the normalized title along with a dictionary of word hashes and their corresponding original words.
    /// </summary>
    /// <param name="title">The title to process. It can be null or empty, in which case the method returns null</param>
    /// <returns>
    /// A tuple containing:
    /// - A 64-bit hash code of the normalized title.
    /// - A dictionary with the hash of each unique word as the key and the original word as the value.
    /// Returns null if the title is invalid or no valid words are found.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static (long Key, Dictionary<long, string> Words)? GetTitleMetadata(string? title) {
        // var dt = DateTime.UtcNow; // for testing

        // try {
            if (string.IsNullOrWhiteSpace(title)) return null;

            var wordsDict = new Dictionary<long, string>();

            foreach (var word in title.Split(Separators, StringSplitOptions.RemoveEmptyEntries)) {
                // we assume that words with meaning start with unicode 19968 (U+4E00 — Chinese character "一", meaning "one".)
                if(word.Length == 1 && (int)word[0] < 19968) 
                    continue;

                string wordLower = word.ToLowerInvariant();
                long wordHash = GetHashCode64(wordLower);

                wordsDict.TryAdd(wordHash, wordLower);
            }

            if (wordsDict.Count == 0) return null;

            string normalizedTitle = string.Join(" ", wordsDict.Values);

            return (GetHashCode64(normalizedTitle), wordsDict);
        // } catch {
        //     var testPoint = true;
        // }

        // return null;
    }
}

    // public static (long Key, Dictionary<long, string> Words)? GetTitleMetadata(string? title) {
    //     // var dt = DateTime.UtcNow; // for testing

    //     // try {
    //         if (string.IsNullOrWhiteSpace(title)) return null;

    //         var separators = new[] { ' ', ',', '.', '!', '?', ':', ';', '-', '_', '"', '\'', '/', '\\', '|', '\t', '\n' };
    //         var wordsDict = new Dictionary<long, string>();

    //         foreach (var word in title.Split(separators, StringSplitOptions.RemoveEmptyEntries)) {
    //             // we assume that words with meaning start with unicode 19968 (U+4E00 — Chinese character "一", meaning "one".)
    //             if(word.Length == 1 && (int)word[0] < 19968) 
    //                 continue;

    //             string wordLower = word.ToLowerInvariant();
    //             long wordHash = GetHashCode64(wordLower);

    //             wordsDict.TryAdd(wordHash, wordLower);
    //         }

    //         if (wordsDict.Count == 0) return null;

    //         string normalizedTitle = string.Join(" ", wordsDict.Values);

    //         return (GetHashCode64(normalizedTitle), wordsDict);
    //     // } catch {
    //     //     var testPoint = true;
    //     // }

    //     // return null;
    // }


    // [MethodImpl(MethodImplOptions.AggressiveInlining)]
    // public static (long Key, Dictionary<long, string> Words)? GetTitleMetadata(string? title) {
    //     if (string.IsNullOrWhiteSpace(title)) return null;

    //     var wordsDict = new Dictionary<long, string>(title.Length / 5); // Примерная оценка среднего кол-ва слов
    //     var span = title.AsSpan();
    //     int start = 0;

    //     for (int i = 0; i <= span.Length; i++)
    //         if (i == span.Length || Separators.Contains(span[i])) {
    //             if (i > start) {
    //                 var word = span[start..i];

    //                 if (word.Length == 1 && word[0] < 19968) {
    //                     start = i + 1;
    //                     continue; // Пропуск незначащих символов
    //                 }

    //                 long wordHash = GetHashCode64(word);
    //                 wordsDict.TryAdd(wordHash, word.ToString());
    //             }
    //             start = i + 1;
    //         }

    //     if (wordsDict.Count == 0) return null;

    //     string normalizedTitle = string.Join(" ", wordsDict.Values);
    //     return (GetHashCode64(normalizedTitle), wordsDict);
    // }