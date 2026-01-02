using System.Collections.Frozen;
using System.Runtime.InteropServices;

namespace Alga.search;
/// <summary>
/// Provides functionality for managing and searching titles, including calculating similarities between titles and storing related word information.
/// </summary>
public static class Titles
{
    private static volatile int _isDirty;
    public static int SetMaxSimilarTitlesInWord { get; set; } = 1000;

    /// <summary>
    /// Returns a list of similar titles based on shared word identifiers.
    /// </summary>
    /// <param name="id">The identifier of the reference title.</param>
    /// <param name="take">The maximum number of similar titles to return. Defaults to 128.</param>
    /// <param name="minSimilar">
    /// The minimum similarity ratio required for a match, 
    /// calculated as (commonWords / totalWords). Defaults to 0.1.
    /// </param>
    /// <returns>
    /// A list of similar title IDs with their corresponding similarity coefficient, 
    /// or <c>null</c> if no sufficient matches are found.
    /// </returns>
    /// <remarks>
    /// This method uses stack-allocated memory for small buffers to minimize allocations and improve performance.
    /// </remarks>
    public static List<(Guid Id, float Coeff)>? SearchSimilarTitlesById(Guid id, int take = 128, float minSimilar = 0.2f)
    {
        EnsureFrozen();

        if (!Collections.TitlesWordMapAsFrozen.TryGetValue(id, out var words)) return null;

        var allKeys = new List<Guid>(words.Length * 500);
        foreach (var word in words)
        {
            if (Collections.WordToTitlesMapAsFrozen.TryGetValue(word, out var articles) && articles != null)
                allKeys.AddRange(articles);
        }

        int wordCount = words.Length;
        int minRequiredMatches = (int)MathF.Ceiling(minSimilar * wordCount);

        var matchCounts = new Dictionary<Guid, int>(allKeys.Count / 2);
        foreach (var key in allKeys)
        {
            ref int count = ref CollectionsMarshal.GetValueRefOrAddDefault(matchCounts, key, out _);
            count++;
        }

        int maxBuffer = Math.Min(take * 2, 512);
        Span<(Guid, float)> buffer = maxBuffer <= 128
            ? stackalloc (Guid, float)[maxBuffer]
            : new (Guid, float)[maxBuffer]; // fallback to heap if needed

        int written = 0;
        foreach (var kvp in matchCounts)
        {
            if (kvp.Value >= minRequiredMatches)
            {
                float coeff = (float)kvp.Value / wordCount;
                if (written < buffer.Length)
                    buffer[written++] = (kvp.Key, coeff);
            }
        }

        if (written == 0) return null;

        var slice = buffer.Slice(0, written);
        slice.Sort((a, b) => b.Item2.CompareTo(a.Item2));

        var result = new List<(Guid, float)>(Math.Min(take, written));
        for (int i = 0; i < slice.Length && result.Count < take; i++)
            result.Add(slice[i]);

        return result;
    }


    public static List<(Guid Id, float Coeff)>? SearchByString(string value, int take = 128, float minSimilar = 0.2f)
    {
        EnsureFrozen();

        var normalizeTitle = Funcs.GetTitleMetadata(value);
        if (normalizeTitle == null || normalizeTitle.Count == 0) return null;

        var allKeys = new List<Guid>(normalizeTitle.Keys.Count * 500);
        foreach (var word in normalizeTitle.Keys)
        {
            if (Collections.WordToTitlesMapAsFrozen.TryGetValue(word, out var articles) && articles != null)
                try { allKeys.AddRange(articles); } catch { }
        }

        int wordCount = normalizeTitle.Keys.Count;
        int minRequiredMatches = (int)MathF.Ceiling(minSimilar * wordCount);

        var matchCounts = new Dictionary<Guid, int>(allKeys.Count / 2);
        foreach (var key in allKeys)
        {
            ref int count = ref CollectionsMarshal.GetValueRefOrAddDefault(matchCounts, key, out _);
            count++;
        }

        int maxBuffer = Math.Min(take * 2, 512);
        Span<(Guid, float)> buffer = maxBuffer <= 128
            ? stackalloc (Guid, float)[maxBuffer]
            : new (Guid, float)[maxBuffer]; // fallback to heap if needed

        int written = 0;
        foreach (var kvp in matchCounts)
        {
            if (kvp.Value >= minRequiredMatches)
            {
                float coeff = (float)kvp.Value / wordCount;
                if (written < buffer.Length)
                    buffer[written++] = (kvp.Key, coeff);
            }
        }

        if (written == 0) return null;

        var slice = buffer.Slice(0, written);
        slice.Sort((a, b) => b.Item2.CompareTo(a.Item2));

        var result = new List<(Guid, float)>(Math.Min(take, written));
        for (int i = 0; i < slice.Length && result.Count < take; i++)
            result.Add(slice[i]);

        return result;
    }


    /// <summary>
    /// Adds a title to an internal list for later analysis, storing metadata and words.
    /// </summary>
    /// <param name="title">The title string to add.</param>
    /// <param name="id">An optional unique identifier for the title.</param>
    /// <returns>True if the title was added successfully; otherwise, false.</returns>
    public static bool TryAdd(string title, Guid? id = null)
    {
        var normalizeTitle = Funcs.GetTitleMetadata(title);
        if (normalizeTitle == null || normalizeTitle.Count == 0) return false;

        var idx = id ?? Funcs.GetDeterministicGuidFromString(title);

        var words = new long[normalizeTitle.Count];
        int i = 0;
        foreach (var word in normalizeTitle)
        {
            Words.TryAdd(word.Key, word.Value);

            var wordEntry = Collections.WordToTitlesMap.GetOrAdd(word.Key, _ => new HashSet<Guid>());

            lock (wordEntry)
                if (wordEntry.Add(idx) && wordEntry.Count > SetMaxSimilarTitlesInWord)
                {
                    var minId = wordEntry.Min();
                    if (minId != idx) wordEntry.Remove(minId);
                }

            words[i++] = word.Key;
        }

        var added = Collections.TitlesWordMap.TryAdd(idx, words);

        if (added) Interlocked.Exchange(ref _isDirty, 1);

        return added;
    }

    private static void EnsureFrozen()
    {
        if (Interlocked.Exchange(ref _isDirty, 0) == 1)
        {
            UpdateWordToTitlesAsFrozen();
            UpdateTitlesWordMapAsFrozen();
        }
    }

    private static void UpdateTitlesWordMapAsFrozen() => Collections.TitlesWordMapAsFrozen = Collections.TitlesWordMap.ToFrozenDictionary();

    private static void UpdateWordToTitlesAsFrozen()
    {
        var snapshot = Collections.WordToTitlesMap.ToArray();

        var frozen = snapshot.ToFrozenDictionary(kv => kv.Key, kv => { lock (kv.Value) return kv.Value.ToArray(); });

        Collections.WordToTitlesMapAsFrozen = frozen;
    }
}


// [MethodImpl(MethodImplOptions.AggressiveInlining)]
// public static List<(long, float)>? GetByIdFull(long id, byte listId = 0, int take = 100, float minSimilar = 0.1f) //, int cacheInMin = 0
// {
//     //var dt = DateTime.UtcNow; // for testing

//     try
//     {
//         var uid = Funcs.CreateUniqueId(listId, id);
//         if (!Collections.TitlesWordMap.TryGetValue(uid, out var astVal)) return null;

//         return GetSearchListFullResult(new(astVal.ToArray()), listId, take, minSimilar);

//         // if (cacheInMin > 0)
//         // {
//         //     var cs = new _Cache.Session(id.ToString(), listId, cacheInMin);
//         //     if (cs.ReturnList is not null) return cs.ReturnList;
//         //     cs.Set(result);
//         // }

//         // return result;
//     }
//     catch
//     {
//         // var testPoint = true;  // for testing
//     }
//     finally
//     {
//         // var workTime = (DateTime.UtcNow - dt).TotalMicroseconds; //  // for testing
//         // var testPoint = true;  // for testing
//     }

//     return null;
// }

/// <summary>
/// Retrieves a list of titles by their value (string), with options for filtering and caching.
/// </summary>
/// <param name="value">The title string to search for.</param>
/// <param name="listId">The list ID (default is 0).</param>
/// <param name="take">The maximum number of results to return (default is 100).</param>
/// <param name="minSimilar">The minimum similarity coefficient to filter titles (default is 0.1).</param>
/// <param name="cacheInMin">The cache expiration time in minutes (default is 0).</param>
/// <returns>A list of title IDs.</returns>
// [MethodImpl(MethodImplOptions.AggressiveInlining)]
// public static List<long>? GetByValue(string? value, byte listId = 0, int take = 100, float minSimilar = 0.2f, int cacheInMin = 0)
// {
//     var dt = DateTime.UtcNow; // for testing

//     try
//     {
//         if (string.IsNullOrWhiteSpace(value)) return null;

//         var cs = new _Cache.Session(value, listId, take, cacheInMin);
//         if (cs.ReturnList is not null) return cs.ReturnList;

//         var normalizeTitle = Funcs.GetTitleMetadata(value);
//         if (normalizeTitle == null || normalizeTitle.Count > 0) return null;

//         foreach (var word in normalizeTitle)
//             _Words.TryAdd(Funcs.GetHashCode64(value), word.Value.Item1, word.Value.Item2);

//         var result = GetSearchListResult(normalizeTitle.Select(i => i.Key).ToHashSet(), listId, take, minSimilar);
//         cs.Set(result);

//         return result;
//     }
//     catch
//     {
//         var testPoint = true;
//     }
//     finally
//     {
//         var workTime = (DateTime.UtcNow - dt).TotalMicroseconds; // For testing
//         var testPoint = true;
//     }

//     return null;
// }


// [MethodImpl(MethodImplOptions.AggressiveInlining)]
// static void CheckAndDeleteOutdateRows()
// {
//     try
//     {
//         if (SetMaxRowNumber <= 0 || Collections.TitlesWordMap.Count < SetMaxRowNumber) return;

//         var minKey = Collections.TitlesWordMap.Min(i => i.Key);
//         if (!Collections.TitlesWordMap.TryRemove(minKey, out _))
//             return;

//         foreach (var word in Collections.WordToTitlesMap)
//         {
//             if (word.Value is null) continue;

//             foreach (var i in word.Value.Keys)
//             {
//                 if (i.Equals(minKey))
//                 {
//                     word.Value.TryRemove(i, out _);
//                     break;
//                 }
//             }
//         }
//     }
//     catch { }
// }

/// <summary>
/// Computes a list of similar articles based on shared words and similarity coefficients.
/// </summary>
/// <param name="words">A set of word hashes for which to find similar articles.</param>
/// <param name="listId">The list ID to consider (default is 0).</param>
/// <returns>A list of similar articles with their similarity coefficients.</returns>
// [MethodImpl(MethodImplOptions.AggressiveInlining)]
// static List<KeyValuePair<long, float>>? GetArticleSimilarList(HashSet<long> words)
// {
//     // var dt = DateTime.UtcNow; // for testing

//     // try {
//     var xl = new List<KeyValuePair<long, float>>();
//     var alx = new Dictionary<long, float>();

//     foreach (var word in words)
//     {
//         var ws = new Dictionary<long, float> { { word, 1 } };
//         if (_Words.SimilarsList.TryGetValue(word, out var wsVal))
//             foreach (var j in wsVal)
//                 ws.Add(j.Key, j.Value);

//         foreach (var j in ws)
//         {
//             if (Collections.WordToTitlesMap.TryGetValue(j.Key, out var aVal))
//                 foreach (var i in aVal)
//                     if (alx.Contains(i.Key))
//                         alx[i.Key] += j.Value;
//                     else alx.TryAdd(i.Key, j.Value);
//         }
//     }

//     foreach (var i in alx.OrderByDescending(i => i.Value))
//         xl.Add(new KeyValuePair<long, float>(i.Key, i.Value / words.Count));

//     return xl;
//     // } catch {
//     //     var testPoint = true;
//     // } finally {
//     //     var workTime = (DateTime.UtcNow - dt).TotalMicroseconds; // For testing
//     //     var testPoint = true;
//     // }

//     //return null;
// }

/// <summary>
/// Retrieves a list of search results based on title IDs and similarity thresholds.
/// </summary>
// [MethodImpl(MethodImplOptions.AggressiveInlining)]
// static List<long>? GetSearchListResult(HashSet<long> valueIds, byte listId, int take, float minSimilar)
// {
//     var matches = GetArticleSimilarList(valueIds);
//     if (matches is null || take <= 0) return null;

//     var result = new List<long>(Math.Min(take, matches.Count));
//     int count = 0;

//     foreach (var kv in matches)
//         if (kv.Value > minSimilar)
//         {
//             result.Add(kv.Key);
//             if (++count >= take)
//                 break;
//         }

//     return result;

//     // var matches = GetArticleSimilarList(valueIds);
//     // return matches?.Where(i => i.Value > minSimilar).Take(take).Select(i => i.Key.Id).ToList();
// }

// static List<(long, float)>? GetSearchListFullResult(HashSet<long> valueIds, byte listId, int take, float minSimilar)
// {
//     var matches = GetArticleSimilarList(valueIds);
//     if (matches is null || take <= 0) return null;

//     var result = new List<(long, float)>(Math.Min(take, matches.Count));
//     int count = 0;

//     foreach (var kv in matches)
//         if (kv.Value > minSimilar)
//         {
//             result.Add((kv.Key, kv.Value));
//             if (++count >= take)
//                 break;
//         }

//     return result;
// }



// [MethodImpl(MethodImplOptions.AggressiveInlining)]
// public static List<(long, float)>? GetSimilarTitlesById(long id, int take = 100, float minSimilar = 0.1f)
// {
//     var dt = DateTime.UtcNow;
//     if (!Collections.TitlesWordMap.TryGetValue(id, out var words)) return null;

//     int estimatedSize = Math.Min(words.Length * 8, 1_000_000); // Ограничиваем максимальный размер
//     var allKeys = new List<long>(estimatedSize);

//     // 1. Быстрое накопление всех ключей
//     foreach (var word in words)
//     {
//         if (!Collections.WordToTitlesMap.TryGetValue(word, out var articles)) continue;

//         allKeys.AddRange(articles);
//     }

//     int wordCount = words.Length;
//     int minRequiredMatches = (int)MathF.Ceiling(minSimilar * wordCount);

//     // 2. Используем Dictionary вместо ConcurrentDictionary для однопоточного подсчета
//     var matchCounts = new Dictionary<long, int>(allKeys.Count / 2); // Уменьшаем начальный размер

//     foreach (var key in allKeys)
//     {
//         ref int count = ref CollectionsMarshal.GetValueRefOrAddDefault(matchCounts, key, out _);
//         count++;
//     }

//     // 3. Отбор и сортировка результатов с оптимизацией
//     int resultSize = Math.Min(take, matchCounts.Count);
//     var buffer = new List<(long Id, float Coefficient)>(resultSize);

//     foreach (var kvp in matchCounts)
//     {
//         if (kvp.Value >= minRequiredMatches)
//         {
//             buffer.Add((kvp.Key, (float)kvp.Value / wordCount));
//         }
//     }

//     // Оптимизированная сортировка с ограничением количества элементов
//     if (buffer.Count > 1)
//     {
//         buffer.Sort((a, b) => b.Coefficient.CompareTo(a.Coefficient));

//         if (buffer.Count > take)
//         {
//             if (take > 0) buffer.RemoveRange(take, buffer.Count - take);
//             else buffer.Clear();
//         }
//     }

//     var dtd = (DateTime.UtcNow - dt).TotalMilliseconds;
//     return buffer.Count > 0 ? buffer : null;
// }