using System.Collections.Frozen;
using System.Runtime.InteropServices;
using System.Collections.Concurrent;

namespace Alga.search;
/// <summary>
/// Provides functionality for managing and searching titles, including calculating similarities between titles and storing related word information.
/// </summary>
public static class Titles
{
    private static bool _isDirty;
    private static readonly object _refreshLock = new();
    private static DateTime _lastAddTitleDt = DateTime.UtcNow;
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
        RefreshCollections();

        if (!Collections.TitlesWordMap.TryGetValue(id, out var words)) return null;

        var allKeys = new List<Guid>(words.Length * 500);
        foreach (var word in words)
        {
            if (Collections.WordToTitlesMap.TryGetValue(word, out var articles) && articles != null)
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
        var dt = DateTime.UtcNow;

        RefreshCollections();

        var normalizeTitle = Funcs.GetTitleMetadata(value);
        if (normalizeTitle == null || normalizeTitle.Count == 0) return null;

        var allKeys = new List<Guid>(normalizeTitle.Keys.Count * 500);
        foreach (var word in normalizeTitle.Keys)
        {
            if (Collections.WordToTitlesMap.TryGetValue(word, out var articles) && articles != null)
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
        Span<(Guid, float)> buffer = maxBuffer <= 128 ? stackalloc (Guid, float)[maxBuffer] : new (Guid, float)[maxBuffer];

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

        var mcs = (DateTime.UtcNow - dt).TotalMicroseconds;

        return result;
    }


    /// <summary>
    /// Adds a title to an internal list for later analysis, storing metadata and words.
    /// </summary>
    /// <param name="title">The title string to add.</param>
    /// <param name="id">An optional unique identifier for the title.</param>
    /// <returns>True if the title was added successfully; otherwise, false.</returns>ss
    public static void TryAdd(string title, Guid? id = null)
    {
        var idx = id ?? Funcs.GetDeterministicGuidFromString(title);
        _isDirty = Collections.TitlesBufferMap.TryAdd(idx, title);

        RefreshCollections();
    }

    /// <summary>
    /// Updates all main collections of titles and words by processing newly added titles from the buffer.
    /// </summary>
    private static void RefreshCollections()
    {
        if (!_isDirty || (DateTime.UtcNow - _lastAddTitleDt).TotalSeconds < 60) return;

        _isDirty = false;
        _lastAddTitleDt = DateTime.UtcNow;

        Dictionary<Guid, string> bufferSnapshot;
        lock (_refreshLock)
        {
            // Сделаем моментальный снимок буфера и очистим оригинал
            bufferSnapshot = Collections.TitlesBufferMap.ToDictionary(kv => kv.Key, kv => kv.Value);
            foreach (var kv in bufferSnapshot)
                Collections.TitlesBufferMap.TryRemove(kv.Key, out _);
        }

        var wordsMap = Collections.WordsMap.ToDictionary();

        var wordSimilarityMap = Collections.WordSimilarityMap.ToDictionary(
            outer => outer.Key,
            outer => new ConcurrentDictionary<long, float>(outer.Value)
        );

        var wordToTitlesMap = Collections.WordToTitlesMap.ToDictionary(
            kv => kv.Key,
            kv => new Queue<Guid>(kv.Value)
        );

        var titlesWordMap = Collections.TitlesWordMap.ToDictionary();

        foreach (var title in bufferSnapshot)
        {
            if (titlesWordMap.ContainsKey(title.Key)) continue;

            var normalizeTitle = Funcs.GetTitleMetadata(title.Value);
            if (normalizeTitle == null || normalizeTitle.Count == 0) continue;

            var words = new long[normalizeTitle.Count];
            int i = 0;

            foreach (var word in normalizeTitle)
            {
                // Добавляем/обновляем WordInfo и WordSimilarity
                Words.TryAdd(word.Key, word.Value, wordsMap, wordSimilarityMap);

                // Обновляем WordToTitlesMap
                if (!wordToTitlesMap.TryGetValue(word.Key, out var queue))
                {
                    queue = new Queue<Guid>();
                    wordToTitlesMap[word.Key] = queue;
                }

                lock (queue)
                {
                    if (!queue.Contains(title.Key))
                    {
                        queue.Enqueue(title.Key);

                        if (queue.Count > SetMaxSimilarTitlesInWord)
                            queue.Dequeue(); // удаляем самый старый
                    }
                }

                words[i++] = word.Key;
            }

            titlesWordMap[title.Key] = words;
        }

        Collections.WordsMap = wordsMap.ToFrozenDictionary();
        Collections.WordSimilarityMap = wordSimilarityMap.ToFrozenDictionary(outer => outer.Key, outer => outer.Value.ToFrozenDictionary());
        Collections.WordToTitlesMap = wordToTitlesMap.ToFrozenDictionary(kv => kv.Key, kv => kv.Value.ToArray());
        Collections.TitlesWordMap = titlesWordMap.ToFrozenDictionary(kv => kv.Key, kv => kv.Value);
    }
}