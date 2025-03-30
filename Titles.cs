using System.Collections.Concurrent;
using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Alga.search;
/// <summary>
/// Provides functionality for managing and searching titles, including calculating similarities between titles and storing related word information.
/// </summary>
public static class Titles {
    /// <summary>
    /// Sets the maximum number of titles to be retrieved in a search.
    /// </summary>
    public static int SetMaxRowNumber { get; set; } = int.MaxValue;
    public static int SetMaxSimilarTitlesInWord { get; set; } = 1000;

    /// <summary>
    /// Represents a unique identifier for a title, combining a list ID and a title ID.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 8)]
    readonly struct TitleKey : IEquatable<TitleKey> {
        public byte ListId { get; }
        public long Id { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="TitleKey"/> struct with the specified list ID and title ID.
        /// </summary>
        /// <param name="listId"></param>
        /// <param name="id"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TitleKey(byte listId, long id) {
            ListId = listId;
            Id = id;
        }

        /// <summary>
        /// Gets the hash code of the TitleKey.
        /// </summary>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode() => HashCode.Combine(ListId, Id);
        /// <summary>
        /// Determines whether the specified object is equal to the current TitleKey.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals(object? obj) => obj is TitleKey other && Equals(other);
        /// <summary>
        /// Determines whether the current TitleKey is equal to another TitleKey.
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(TitleKey other) => ListId == other.ListId && Id == other.Id;
    }

    /// <summary>
    /// A list of titles with basic information, including a hash code (unique identifier) and a set of unique words contained in the title.
    /// Additionally, includes a dictionary of similar titles with their respective similarity coefficients.
    /// Average row size: 232 bytes.
    /// </summary>
    static readonly ConcurrentDictionary<TitleKey, ReadOnlyMemory<long>> BaseList = new(concurrencyLevel: Environment.ProcessorCount, capacity: 10000);

    /// <summary>
    /// A list of words and the titles that contain these words.
    /// The key is the word's hash code, and the value is a list of title IDs (long).
    /// Average row size: 1280 bytes.
    /// </summary>
    static readonly ConcurrentDictionary<long, ConcurrentDictionary<TitleKey, byte>> WordsList = new(concurrencyLevel: Environment.ProcessorCount, capacity: 10000);

    /// <summary>
    /// Retrieves a list of titles by their ID, with filtering options for the number of results and similarity threshold.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="listId"></param>
    /// <param name="take"></param>
    /// <param name="minSimilar"></param>
    /// <param name="cacheInMin"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static List<long>? GetById(long id, byte listId=0, int take=100, float minSimilar=0.1f, int cacheInMin = 0) {
        var dt = DateTime.UtcNow; // for testing
        
        try {
            if (!BaseList.TryGetValue(new TitleKey(0, id), out var astVal) || astVal.IsEmpty)
                return null;

            var cs = new _Cache.Session(id.ToString(), listId, cacheInMin);
            if(cs.ReturnList is not null) return cs.ReturnList;

            var result = GetSearchListResult(new (astVal.ToArray()), listId, take, minSimilar);
            cs.Set(result);

            return result;
        } catch {
            var testPoint = true;
        } finally {
            var workTime = (DateTime.UtcNow - dt).TotalMicroseconds; // For testing
            var testPoint = true;
        }

        return null;
    }

    /// <summary>
    /// Retrieves a list of titles by their value (string), with options for filtering and caching.
    /// </summary>
    /// <param name="value">The title string to search for.</param>
    /// <param name="listId">The list ID (default is 0).</param>
    /// <param name="take">The maximum number of results to return (default is 100).</param>
    /// <param name="minSimilar">The minimum similarity coefficient to filter titles (default is 0.1).</param>
    /// <param name="cacheInMin">The cache expiration time in minutes (default is 0).</param>
    /// <returns>A list of title IDs.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static List<long>? GetByValue(string? value, byte listId = 0, int take = 100, float minSimilar = 0.2f, int cacheInMin = 0) {
        var dt = DateTime.UtcNow; // for testing
        
        try {
            if (string.IsNullOrWhiteSpace(value)) return null;

            var cs = new _Cache.Session(value, listId, take, cacheInMin);
            if(cs.ReturnList is not null) return cs.ReturnList;

            var normalizeTitle = Funcs.GetTitleMetadata(value);
            if (!normalizeTitle.HasValue) return null;

            var words = normalizeTitle.Value.Words;
            foreach(var word in words)
                _Words.TryAdd(word.Key, word.Value);
            
            var result = GetSearchListResult(words.Select(i => i.Key).ToHashSet(), listId, take, minSimilar);
            cs.Set(result);

            return result;
        } catch {
            var testPoint = true;
        } finally {
            var workTime = (DateTime.UtcNow - dt).TotalMicroseconds; // For testing
            var testPoint = true;
        }

        return null;
    }

    // public static long GetHashCode(string title) {
    //     var normalizeTitle = Funcs.GetTitleMetadata(title);
    //     if (!normalizeTitle.HasValue) return 0;

    //     return normalizeTitle.Value.Key;
    // }

    /// <summary>
    /// Adds a title to an internal list for later analysis, storing metadata and words.
    /// </summary>
    /// <param name="title">The title string to add.</param>
    /// <param name="searchCategoryId">The search category ID (default is 0).</param>
    /// <param name="id">An optional unique identifier for the title.</param>
    /// <returns>True if the title was added successfully; otherwise, false.</returns>
    public static bool TryAdd(string title, long? id = null, byte searchCategoryId = 0) {
        if (string.IsNullOrWhiteSpace(title)) return false;

        CheckAndDeleteOutdateRows();

        var normalizeTitle = Funcs.GetTitleMetadata(title);
        if (!normalizeTitle.HasValue) return false;

        long idx = id ?? normalizeTitle.Value.Key; //GetHashCode(title);
        var idBox = new TitleKey(searchCategoryId, idx);

        var words = new long[normalizeTitle.Value.Words.Count];
        int i = 0;
        foreach (var word in normalizeTitle.Value.Words)
        {
            _Words.TryAdd(word.Key, word.Value);
            
            var wordEntry = WordsList.GetOrAdd(word.Key, _ => new ConcurrentDictionary<TitleKey, byte>(
                concurrencyLevel: 1,
                capacity: SetMaxSimilarTitlesInWord / 2));

            wordEntry[idBox] = 0;

            if (wordEntry.Count > SetMaxSimilarTitlesInWord)
            {
                var artMinId = wordEntry.MinBy(i => i.Key.Id).Key;
                wordEntry.TryRemove(artMinId, out _);
            }

            words[i++] = word.Key;
        }

        return BaseList.TryAdd(idBox, words);

        // var words = new HashSet<long>();
        // foreach (var word in normalizeTitle.Value.Words) {
        //     _Words.TryAdd(word.Key, word.Value);

        //     var wordEntry = WordsList.GetOrAdd(word.Key, _ => new ConcurrentDictionary<TitleKey, byte>());
        //     //wordEntry[idBox] = 0;
        //     wordEntry.TryAdd(idBox, 0);

        //     if (wordEntry.Count > SetMaxSimilarTitlesInWord) {
        //         var artMinId = wordEntry.MinBy(i => i.Key.Id).Key;
        //         wordEntry.TryRemove(artMinId, out _);
        //     }

        //     words.Add(word.Key);
        // }

        // return BaseList.TryAdd(idBox, new ReadOnlyMemory<long>(words.ToArray()));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static void CheckAndDeleteOutdateRows() {
        try {
            if (SetMaxRowNumber <= 0 || BaseList.Count < SetMaxRowNumber) return;

            var minKey = BaseList.Min(i => i.Key);
            if (!BaseList.TryRemove(minKey, out _))
                return;

            foreach (var word in WordsList) {
                if (word.Value is null) continue;

                foreach (var i in word.Value.Keys) {
                    if (i.Equals(minKey)) {
                        word.Value.TryRemove(i, out _);
                        break;
                    }
                }
            }
        } catch { }
    }

    /// <summary>
    /// Computes a list of similar articles based on shared words and similarity coefficients.
    /// </summary>
    /// <param name="words">A set of word hashes for which to find similar articles.</param>
    /// <param name="listId">The list ID to consider (default is 0).</param>
    /// <returns>A list of similar articles with their similarity coefficients.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static List<KeyValuePair<TitleKey, float>>? GetArticleSimilarList(HashSet<long> words) {
        // var dt = DateTime.UtcNow; // for testing
        
        // try {
            var xl = new List<KeyValuePair<TitleKey, float>>();
            var alx = new Dictionary<TitleKey, float>();

            foreach (var word in words) {
                var ws = new Dictionary<long, float> { { word, 1 } };
                if (_Words.SimilarsList.TryGetValue(word, out var wsVal))
                    foreach (var j in wsVal)
                        ws.Add(j.Key, j.Value);

                foreach (var j in ws) {
                    if (WordsList.TryGetValue(j.Key, out var aVal))
                        foreach(var i in aVal)
                            if(alx.ContainsKey(i.Key)) 
                                alx[i.Key] += j.Value;
                            else alx.TryAdd(i.Key, j.Value);
                }
            }

            foreach (var i in alx.OrderByDescending(i => i.Value))
                xl.Add(new KeyValuePair<TitleKey, float>(i.Key, i.Value / words.Count));

            return xl;
        // } catch {
        //     var testPoint = true;
        // } finally {
        //     var workTime = (DateTime.UtcNow - dt).TotalMicroseconds; // For testing
        //     var testPoint = true;
        // }

        //return null;
    }

    /// <summary>
    /// Retrieves a list of search results based on title IDs and similarity thresholds.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static List<long>? GetSearchListResult(HashSet<long> valueIds, byte listId, int take, float minSimilar) {
        var matches = GetArticleSimilarList(valueIds);
        return matches?.Where(i => i.Value > minSimilar).Take(take).Select(i => i.Key.Id).ToList();
    }
}





// using System.Collections.Concurrent;
// using System.Buffers;
// using Microsoft.Extensions.Caching.Memory;


// namespace Alga.search;
// public class Titles {
//     public readonly struct TitleKey : IEquatable<TitleKey> {
//         public byte ListId { get; }
//         public long Id { get; }

//         public TitleKey(byte listId, long id) {
//             ListId = listId;
//             Id = id;
//         }

//         public override int GetHashCode() => HashCode.Combine(ListId, Id);
//         public override bool Equals(object? obj) => obj is TitleKey other && Equals(other);
//         public bool Equals(TitleKey other) => ListId == other.ListId && Id == other.Id;
//     }

//     /// <summary>
//     /// Titles list with base information about it
//     /// Hash code of the title (unique identifier)
//     /// Set of unique words contained in the title
//     /// Dictionary of similar titles with a similarity coefficient
//     /// Size (average row length): 232 byte
//     /// </summary>
//     static readonly ConcurrentDictionary<TitleKey, ReadOnlyMemory<long>> BaseList = new();

//     /// <summary>
//     /// A list of words with information about which titles contain this word
//     /// Where: Key is Word HashCode, Value is list of Title Id (long is titlle)
//     /// Size (average row length): 1280 byte
//     /// </summary>
//     static readonly ConcurrentDictionary<long, ConcurrentDictionary<TitleKey, byte>> WordsList = new ();
    
//     private static readonly MemoryCache Cache = new(new MemoryCacheOptions());

//     public static List<long>? SearchById(long id, byte listId = 0, int take = 100, float minSimilar = 0.1f, int cacheInMin = 0) {
//         if (!BaseList.TryGetValue(new TitleKey(0, id), out var astVal) || astVal.IsEmpty)
//             return null;

//         string cacheKey = $"Search_{id}_{listId}_{take}";
//         if (cacheInMin > 0 && Cache.TryGetValue(cacheKey, out List<long>? cachedResult))
//             return cachedResult;

//         //var result = GetSearchListResult(astVal.Span.ToArray(), listId, take, minSimilar);
//         var result = GetSearchListResult(new HashSet<long>(astVal.ToArray()), listId, take, minSimilar);
//         SetCache(cacheInMin, cacheKey, result);
//         return result;
//     }

//     // public static List<long>? SearchById(long id, byte ListId = 0, int take = 100, float minSimilar = 0.1f, int cacheInMin = 0) {
//     //     if (!BaseList.TryGetValue(new TitleKey(0, id), out var astVal) || astVal.IsEmpty)
//     //         return null;

//     //     string cacheKey = $"Search_{id}_{ListId}_{take}";

//     //     if (cacheInMin > 0 && _cache.TryGetValue(cacheKey, out List<long>? cachedResult))
//     //         return cachedResult;

//     //     var result = GetSearchListResult(astVal.ToArray().ToHashSet(), ListId, take, minSimilar);
//     //     SetCache(cacheInMin, cacheKey, result);
//     //     return result;
//     // }

//     // public static List<long>? SearchById(long id, byte ListId=0, int take=100, float minSimilar=0.1f, int cacheInMin = 0) {
//     //     if(!BaseList.TryGetValue(new Alga.search.Titles.TitleKey(0, id), out var astVal) || astVal == null) return null;

//     //     string cacheKey = $"Search_{id.ToString()}_{ListId}_{take}";

//     //     if (cacheInMin > 0 && _cache.TryGetValue(cacheKey, out List<long>? cachedResult)) 
//     //         return cachedResult;

//     //     var result = GetSearchListResult(astVal.ToHashSet(), ListId, take, minSimilar);

//     //     SetCache(cacheInMin, cacheKey, result);

//     //     return result;
//     // }

//     public static List<long>? Search(string? searchValue, byte listId = 0, int take = 100, float minSimilar = 0.1f, int cacheInMin = 0)
//     {
//         if (string.IsNullOrWhiteSpace(searchValue)) return null;

//         string cacheKey = $"Search_{searchValue}_{listId}_{take}";
//         if (cacheInMin > 0 && Cache.TryGetValue(cacheKey, out List<long>? cachedResult))
//             return cachedResult;

//         var normalizeTitle = Funcs.GetTitleMetadata(searchValue);
//         if (!normalizeTitle.HasValue) return null;

//         var words = normalizeTitle.Value.Words.Select(i => i.Key).ToHashSet();
//         var result = GetSearchListResult(words, listId, take, minSimilar);
//         SetCache(cacheInMin, cacheKey, result);
//         return result;
//     }
    
//     // public static List<long>? Search(string? searchValue, byte ListId=0, int take=100, float minSimilar=0.1f, int cacheInMin = 0) {
//     //     var dt = DateTime.UtcNow; // for testing

//     //     try {
//     //         if (string.IsNullOrWhiteSpace(searchValue)) return null;

//     //         string cacheKey = $"Search_{searchValue}_{ListId}_{take}";

//     //         if (cacheInMin > 0 && _cache.TryGetValue(cacheKey, out List<long>? cachedResult)) 
//     //             return cachedResult;

//     //         var normalizeTitle = Funcs.GetTitleMetadata(searchValue);

//     //         if (normalizeTitle.HasValue != true) return null;

//     //         var words = normalizeTitle.Value.Words.Select(i => i.Key).ToHashSet();

//     //         var result = GetSearchListResult(words, ListId, take, minSimilar);

//     //         SetCache(cacheInMin, cacheKey, result);

//     //         return result;    
//     //     } catch {
//     //         var testPoint = true;
//     //     } finally {
//     //         var workTime = (DateTime.UtcNow - dt).TotalMicroseconds; // For testing
//     //         var testPoint = true;
//     //     }

//     //     return null;
//     // }

//     private static List<long>? GetSearchListResult(HashSet<long> valueIds, byte listId, int take, float minSimilar) {
//         var matches = GetArticleSimilarList(valueIds, listId);
//         return matches?.Where(i => i.Value > minSimilar).Take(take).Select(i => i.Key.Id).ToList();
//     }

//     // static List<long>? GetSearchListResult(HashSet<long> valueIds, byte ListId, int take, float minSimilar) {
//     //     var matches = GetArticleSimilarList(valueIds, ListId);
//     //     return matches?.Where(i => i.Value > minSimilar).Take(take).Select(i => i.Key.Id).ToList();
//     // }

//     // static List<long>? GetSearchListResult(HashSet<long> valueIds, byte ListId=0, int take=100, float minSimilar=0.1f) {
//     //     var dt = DateTime.UtcNow; // for testing

//     //     try {
//     //         var l = GetArticleSimilarList(valueIds, ListId);

//     //         if (l == null || !l.Any()) return null;

//     //         return l.Where(i=>i.Value > minSimilar).Take(take).Select(i => i.Key).Select(i => i.Id).ToList();            
//     //     } catch {
//     //         var testPoint = true;
//     //     } finally {
//     //         var workTime = (DateTime.UtcNow - dt).TotalMicroseconds; // For testing
//     //         var testPoint = true;
//     //     }

//     //     return null;
//     // }

//     /// <summary>
//     /// Adding a title to an internal list for later analysis
//     /// </summary>
//     /// <param name="title">строка / заголовок</param>
//     /// <param name="id">Id - если есть уникальный идентификатор строки / заголовка (наприер из внешней базы жанных)</param>
//     /// <param name="id">maxNumberInSimilarList - определяет какое максиммальное количество похожих заголовков можно хранить для этого id если вы планируете искать их по id (title)</param>
//     /// <returns></returns>
//     public static bool TryAdd(string title, byte searchCategoryId = 0, long? id = null)
//     {
//         if (string.IsNullOrWhiteSpace(title)) return false;

//         var normalizeTitle = Funcs.GetTitleMetadata(title);
//         if (!normalizeTitle.HasValue) return false;

//         long idx = id ?? normalizeTitle.Value.Key;
//         var idBox = new TitleKey(searchCategoryId, idx);

//         var words = new HashSet<long>();
//         foreach (var word in normalizeTitle.Value.Words) {
//             var wordEntry = WordsList.GetOrAdd(word.Key, _ => new ConcurrentDictionary<TitleKey, byte>());
//             wordEntry.TryAdd(idBox, 0);

//             if (wordEntry.Count > 500) {
//                 var minKey = wordEntry.MinBy(i => i.Key.Id).Key;
//                 wordEntry.TryRemove(minKey, out _);
//             }

//             words.Add(word.Key);
//         }

//         return BaseList.TryAdd(idBox, new ReadOnlyMemory<long>(words.ToArray()));
//     }

//     // public static bool TryAdd(string title, byte searchCategoryId = 0, long? id = null) {
//     //     if (string.IsNullOrWhiteSpace(title)) return false;

//     //     var normalizeTitle = Funcs.GetTitleMetadata(title);
//     //     if (!normalizeTitle.HasValue) return false;

//     //     long idx = id ?? normalizeTitle.Value.Key;
//     //     var idBox = new TitleKey(searchCategoryId, idx);

//     //     var words = new HashSet<long>();
//     //     foreach (var word in normalizeTitle.Value.Words) {
//     //         var wordEntry = WordsList.GetOrAdd(word.Key, _ => new ConcurrentDictionary<TitleKey, byte>());
//     //         wordEntry.TryAdd(idBox, 0);

//     //         if (wordEntry.Count > 500) {
//     //             var artMinId = wordEntry.MinBy(i => i.Key.Id).Key;
//     //             wordEntry.TryRemove(artMinId, out _);
//     //         }

//     //         words.Add(word.Key);
//     //     }

//     //     return BaseList.TryAdd(idBox, new ReadOnlyMemory<long>(words.ToArray()));
//     // }

//     // public static bool TryAdd(string title, byte searchCategoryId = 0, long? id = null, int maxNumberInSimilarList=0) {
//     //     var dt = DateTime.UtcNow; // for testing

//     //     try {
//     //         if (string.IsNullOrWhiteSpace(title)) return false;

//     //         var normalizeTitle = Funcs.GetTitleMetadata(title);

//     //         if (normalizeTitle.HasValue != true) return false;

//     //         long idx = id ?? normalizeTitle.Value.Key;

//     //         var idBox = new TitleKey(searchCategoryId, idx);

//     //         // Add new words or update it

//     //         var words = new HashSet<long>();

//     //         foreach (var word in normalizeTitle.Value.Words) {
//     //             var f = Words.TryAdd(word.Key, word.Value);

//     //             var wordEntry = WordsList.GetOrAdd(word.Key, _ => new ConcurrentDictionary<TitleKey, byte>());
//     //             wordEntry.TryAdd(idBox, 0);

//     //             if (wordEntry.Count > 500) {
//     //                 var artMinId = wordEntry.MinBy(i => i.Key.Id).Key;
//     //                 wordEntry.TryRemove(artMinId, out _);
//     //             }

//     //             words.Add(word.Key);
//     //         }

//     //         if (BaseList.TryAdd(idBox, words.ToFrozenSet())) return true;

//     //         return false;
//     //     } catch {
//     //         var testPoint = true;
//     //     } finally {
//     //         var workTime = (DateTime.UtcNow - dt).TotalMicroseconds; // For testing
//     //         var testPoint = true;
//     //     }

//     //     return false;
//     // }

//     // static List<KeyValuePair<TitleKey, float>>? GetArticleSimilarList(HashSet<long> words, byte listId) {
//     //     var dt = DateTime.UtcNow; // for testing
        
//     //     try {
//     //         var xl = new List<KeyValuePair<TitleKey, float>>();
//     //         //var al = new Dictionary<TitleKey, (float Res, float ResLast, float SimLast, long WordIdLast)>();

//     //         var alx = new Dictionary<TitleKey, float>();

//     //         var ind = 0;
//     //         foreach (var word in words) {
//     //             var ws = new Dictionary<long, float> { { word, 1 } };
//     //             if (Words.SimilarsList.TryGetValue(word, out var wsVal))
//     //                 foreach (var j in wsVal)
//     //                     ws.Add(j.Key, j.Value);
//     //             else {
//     //                 // получаемм
//     //             }

//     //             foreach (var j in ws) {
//     //                 if (WordsList.TryGetValue(j.Key, out var aVal))
//     //                     foreach(var i in aVal)
//     //                         if(alx.ContainsKey(i.Key)) 
//     //                             alx[i.Key] += j.Value;
//     //                         else alx.TryAdd(i.Key, j.Value);
//     //             }
//     //         }

//     //         foreach (var i in alx.OrderByDescending(i => i.Value))
//     //             xl.Add(new KeyValuePair<TitleKey, float>(i.Key, i.Value / words.Count));

//     //         return xl;
//     //     } catch {
//     //         var testPoint = true;
//     //     } finally {
//     //         var workTime = (DateTime.UtcNow - dt).TotalMicroseconds; // For testing
//     //         var testPoint = true;
//     //     }

//     //     return null;
//     // }

//     private static List<KeyValuePair<TitleKey, float>>? GetArticleSimilarList(HashSet<long> words, byte listId)
//     {
//         var similarities = new ConcurrentDictionary<TitleKey, float>();
        
//         foreach (var word in words)
//             if (WordsList.TryGetValue(word, out var titles))
//                 foreach (var title in titles.Keys)
//                     similarities.AddOrUpdate(title, 1, (_, v) => v + 1);

//         return similarities.OrderByDescending(i => i.Value)
//                            .Select(i => new KeyValuePair<TitleKey, float>(i.Key, i.Value / words.Count))
//                            .ToList();
//     }

//     // static List<KeyValuePair<TitleKey, float>>? GetArticleSimilarList(HashSet<long> words, byte listId) {
//     //     var similarities = new Dictionary<TitleKey, float>();

//     //     foreach (var word in words) {
//     //         if (WordsList.TryGetValue(word, out var titles)) {
//     //             foreach (var title in titles) {
//     //                 if (similarities.ContainsKey(title.Key))
//     //                     similarities[title.Key] += 1;
//     //                 else
//     //                     similarities[title.Key] = 1;
//     //             }
//     //         }
//     //     }

//     //     return similarities.OrderByDescending(i => i.Value)
//     //                        .Select(i => new KeyValuePair<TitleKey, float>(i.Key, i.Value / words.Count))
//     //                        .ToList();
//     // }

//     private static void SetCache(int cacheInMin, string cacheKey, List<long>? result)
//     {
//         if (cacheInMin > 0 && result is { Count: > 0 })
//         {
//             Cache.Set(cacheKey, result, new MemoryCacheEntryOptions
//             {
//                 AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(cacheInMin)
//             });
//         }
//     }

//     // static void SetCache(int cacheInMin, string cacheKey, List<long>? result) {
//     //     if (cacheInMin > 0 && result is { Count: > 0 }) {
//     //         _cache.Set(cacheKey, result, new MemoryCacheEntryOptions {
//     //             AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(cacheInMin),
//     //             SlidingExpiration = TimeSpan.FromMinutes(cacheInMin)
//     //         });
//     //     }
//     // }

//     // static void SetCache(int cacheInMin, string cacheKey, List<long>? result) {
//     //     if (cacheInMin <= 0 || result == null || !result.Any()) return;

//     //     _cache.Set(cacheKey, result, new MemoryCacheEntryOptions {
//     //         AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(cacheInMin), // Cache for 10 minutes
//     //         SlidingExpiration = TimeSpan.FromMinutes(cacheInMin) // Extend cache if accessed
//     //     });
//     // }
// }









// using System.Collections.Frozen;
// using System.Collections.Concurrent;
// using Microsoft.Extensions.Caching.Memory;


// namespace Alga.search;
// public class Titles {
//     public readonly struct TitleKey : IEquatable<TitleKey> {
//         public byte ListId { get; }
//         public long Id { get; }

//         public TitleKey(byte listId, long id) {
//             ListId = listId;
//             Id = id;
//         }

//         public override int GetHashCode() => HashCode.Combine(ListId, Id);
//         public override bool Equals(object? obj) => obj is TitleKey other && Equals(other);
//         public bool Equals(TitleKey other) => ListId == other.ListId && Id == other.Id;
//     }

//     /// <summary>
//     /// Titles list with base information about it
//     /// Hash code of the title (unique identifier)
//     /// Set of unique words contained in the title
//     /// Dictionary of similar titles with a similarity coefficient
//     /// Size (average row length): 232 byte
//     /// </summary>
//     static readonly ConcurrentDictionary<TitleKey, ReadOnlyMemory<long>> BaseList = new();

//     /// <summary>
//     /// A list of words with information about which titles contain this word
//     /// Where: Key is Word HashCode, Value is list of Title Id (long is titlle)
//     /// Size (average row length): 1280 byte
//     /// </summary>
//     static readonly ConcurrentDictionary<long, ConcurrentDictionary<TitleKey, byte>> WordsList = new ();
    

//     static readonly MemoryCache _cache = new(new MemoryCacheOptions());

//     public static List<long>? SearchById(long id, byte ListId=0, int take=100, float minSimilar=0.1f, int cacheInMin = 0) {
//         if(!BaseList.TryGetValue(new Alga.search.Titles.TitleKey(0, id), out var astVal) || astVal == null) return null;

//         string cacheKey = $"Search_{id.ToString()}_{ListId}_{take}";

//         if (cacheInMin > 0 && _cache.TryGetValue(cacheKey, out List<long>? cachedResult)) 
//             return cachedResult;

//         var result = GetSearchListResult(astVal.ToHashSet(), ListId, take, minSimilar);

//         SetCache(cacheInMin, cacheKey, result);

//         return result;
//     }
    
//     public static List<long>? Search(string? searchValue, byte ListId=0, int take=100, float minSimilar=0.1f, int cacheInMin = 0) {
//         var dt = DateTime.UtcNow; // for testing

//         try {
//             if (string.IsNullOrWhiteSpace(searchValue)) return null;

//             string cacheKey = $"Search_{searchValue}_{ListId}_{take}";

//             if (cacheInMin > 0 && _cache.TryGetValue(cacheKey, out List<long>? cachedResult)) 
//                 return cachedResult;

//             var normalizeTitle = Funcs.GetTitleMetadata(searchValue);

//             if (normalizeTitle.HasValue != true) return null;

//             var words = normalizeTitle.Value.Words.Select(i => i.Key).ToHashSet();

//             var result = GetSearchListResult(words, ListId, take, minSimilar);

//             SetCache(cacheInMin, cacheKey, result);

//             return result;    
//         } catch {
//             var testPoint = true;
//         } finally {
//             var workTime = (DateTime.UtcNow - dt).TotalMicroseconds; // For testing
//             var testPoint = true;
//         }

//         return null;
//     }

//     static List<long>? GetSearchListResult(HashSet<long> valueIds, byte ListId=0, int take=100, float minSimilar=0.1f) {
//         var dt = DateTime.UtcNow; // for testing

//         try {
//             var l = GetArticleSimilarList(valueIds, ListId);

//             if (l == null || !l.Any()) return null;

//             return l.Where(i=>i.Value > minSimilar).Take(take).Select(i => i.Key).Select(i => i.Id).ToList();            
//         } catch {
//             var testPoint = true;
//         } finally {
//             var workTime = (DateTime.UtcNow - dt).TotalMicroseconds; // For testing
//             var testPoint = true;
//         }

//         return null;
//     }

//     /// <summary>
//     /// Adding a title to an internal list for later analysis
//     /// </summary>
//     /// <param name="title">строка / заголовок</param>
//     /// <param name="id">Id - если есть уникальный идентификатор строки / заголовка (наприер из внешней базы жанных)</param>
//     /// <param name="id">maxNumberInSimilarList - определяет какое максиммальное количество похожих заголовков можно хранить для этого id если вы планируете искать их по id (title)</param>
//     /// <returns></returns>
//     public static bool TryAdd(string title, byte searchCategoryId = 0, long? id = null, int maxNumberInSimilarList=0) {
//         var dt = DateTime.UtcNow; // for testing

//         try {
//             if (string.IsNullOrWhiteSpace(title)) return false;

//             var normalizeTitle = Funcs.GetTitleMetadata(title);

//             if (normalizeTitle.HasValue != true) return false;

//             long idx = id ?? normalizeTitle.Value.Key;

//             var idBox = new TitleKey(searchCategoryId, idx);

//             // Add new words or update it

//             var words = new HashSet<long>();

//             foreach (var word in normalizeTitle.Value.Words) {
//                 var f = Words.TryAdd(word.Key, word.Value);

//                 var wordEntry = WordsList.GetOrAdd(word.Key, _ => new ConcurrentDictionary<TitleKey, byte>());
//                 wordEntry.TryAdd(idBox, 0);

//                 if (wordEntry.Count > 500) {
//                     var artMinId = wordEntry.MinBy(i => i.Key.Id).Key;
//                     wordEntry.TryRemove(artMinId, out _);
//                 }

//                 words.Add(word.Key);
//             }

//             if (BaseList.TryAdd(idBox, words.ToFrozenSet())) return true;

//             return false;
//         } catch {
//             var testPoint = true;
//         } finally {
//             var workTime = (DateTime.UtcNow - dt).TotalMicroseconds; // For testing
//             var testPoint = true;
//         }

//         return false;
//     }

//     static List<KeyValuePair<TitleKey, float>>? GetArticleSimilarList(HashSet<long> words, byte listId) {
//         var dt = DateTime.UtcNow; // for testing
        
//         try {
//             var xl = new List<KeyValuePair<TitleKey, float>>();
//             //var al = new Dictionary<TitleKey, (float Res, float ResLast, float SimLast, long WordIdLast)>();

//             var alx = new Dictionary<TitleKey, float>();

//             var ind = 0;
//             foreach (var word in words) {
//                 var ws = new Dictionary<long, float> { { word, 1 } };
//                 if (Words.SimilarsList.TryGetValue(word, out var wsVal))
//                     foreach (var j in wsVal)
//                         ws.Add(j.Key, j.Value);
//                 else {
//                     // получаемм
//                 }

//                 foreach (var j in ws) {
//                     if (WordsList.TryGetValue(j.Key, out var aVal))
//                         foreach(var i in aVal)
//                             if(alx.ContainsKey(i.Key)) 
//                                 alx[i.Key] += j.Value;
//                             else alx.TryAdd(i.Key, j.Value);
//                 }
//             }

//             foreach (var i in alx.OrderByDescending(i => i.Value))
//                 xl.Add(new KeyValuePair<TitleKey, float>(i.Key, i.Value / words.Count));

//             return xl;
//         } catch {
//             var testPoint = true;
//         } finally {
//             var workTime = (DateTime.UtcNow - dt).TotalMicroseconds; // For testing
//             var testPoint = true;
//         }

//         return null;
//     }

//     static void SetCache(int cacheInMin, string cacheKey, List<long>? result) {
//         if (cacheInMin <= 0 || result == null || !result.Any()) return;

//         _cache.Set(cacheKey, result, new MemoryCacheEntryOptions {
//             AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(cacheInMin), // Cache for 10 minutes
//             SlidingExpiration = TimeSpan.FromMinutes(cacheInMin) // Extend cache if accessed
//         });
//     }
// }