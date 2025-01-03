using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Alga.search;
public class Titles {
    /// <summary>
    /// 
    /// </summary>
    /// <param name="titles"></param>
    /// <param name="titleWords"> title (id / HashCode) list, where byte always is 0</param>
    /// <param name="title"></param>
    /// <param name="id"></param>
    /// <param name="maxSimCoefToAdd"></param>
    /// <returns></returns>
    public static bool TryAddInTitleDictionary(ref ConcurrentDictionary<long, ValueModel> titles, ref ConcurrentDictionary<int, ConcurrentDictionary<long, byte>> titleWords, string title, long? id = null, double maxSimCoefToAdd = 1) {
        var normalizeTitle = Funcs.NormalizedTitle.GetBase(title);
        if (normalizeTitle == null || !normalizeTitle.HasValue) return false;

        //var normalizeTitleWordsNuber = normalizeTitle.Value.words.Count;

        long idx = id ?? normalizeTitle.Value.title.GetHashCode();

        // Add new word or updte it

        foreach (var word in normalizeTitle.Value.words)
            if(Words.TryInWordDictionaryOrUpdate(word.Value) != null)
                titleWords.GetOrAdd(word.Key, _ => new ConcurrentDictionary<long, byte>()).TryAdd(idx, 0);


        // Making "combinations of words" that we assume have a currently title

        var taws = new HashSet<int[]>(new SequenceEqualityComparer<int>());
        taws.Add(normalizeTitle.Value.words.Select(w => w.Key).ToArray());

        var newCombinations = new List<int[]>();

        foreach (var word in normalizeTitle.Value.words) {
            if (!Words.List.TryGetValue(word.Value, out var wordVal)) continue;

            foreach (var simWord in wordVal.Simillars.Where(sw => sw.Value >= 0.8)) {
                int hashCode = simWord.Key.GetHashCode();

                foreach (var combination in taws.ToList()) { // Создаем копию для безопасной итерации
                    if (!combination.Contains(word.Key)) continue;

                    var newCombination = combination.Select(k => k == word.Key ? hashCode : k).ToArray();
                    newCombinations.Add(newCombination);
                }
            }
        }

        // Adding new combinations after the iteration is completed

        foreach (var combination in newCombinations)
            taws.Add(combination);

        // Making combinations of titles that can include words from the "combinations of words"

        var artls = new HashSet<List<long>>();

        foreach(var i in taws) {
            var tl = new List<long>();
            foreach(var j in i)
                if(titleWords.TryGetValue(j, out var val)) {
                    var ly = val.Keys.OrderByDescending(i=>i).Where(k => k != idx).Take(1000);  // to test
                    tl.AddRange(ly);
                }

            if(tl.Count > 0) artls.Add(tl);
        }

        // We count how many times each "titles id" is mentioned in each combination

        var artlmtxs = new HashSet<Dictionary<long, int>>();

        foreach(var tl in artls) {
            var lstd = new Dictionary<long, int>();
            var tlor = tl.OrderBy(i=>i);

            long current = tlor.First();
            int count = 0;

            foreach(var i in tlor)
                if (i == current) count++;
                else {
                    lstd[current] = count;
                    current = i;
                    count = 1;
                }

            lstd[current] = count;

            artlmtxs.Add(lstd.OrderByDescending(i => i.Value).Take(1000).ToDictionary());
        }

        // We get rid of combinations and get a list of similar titles

        var artlmtxXs = new Dictionary<long, int>();
        foreach(var tl in artlmtxs)
            foreach(var v in tl)
                if(artlmtxXs.TryGetValue(v.Key, out var val)) {
                    if(val < v.Value)
                        val = v.Value;
                } else artlmtxXs.TryAdd(v.Key, v.Value);

        // We get rid of combinations and get a list of similar titles with coefficents
        
        var lsth = new Dictionary<long, double>();
        if(artlmtxXs.Count > 0)
            foreach (var i in artlmtxXs)
                if(titles.TryGetValue(i.Key, out var value))
                    lsth.TryAdd(i.Key, Funcs.GetMatchCoefficient(i.Value, normalizeTitle.Value.words.Count, value.WordNumber));

        // нужна прокладочка если довольно много одинаковых коэффицентов

        // -----

        var lstg = new ConcurrentDictionary<long, double>();
        foreach(var i in lsth.Where(i=>i.Value > 0.1).OrderByDescending(i=>i.Value).Take(100)) {
            if(i.Value > maxSimCoefToAdd) { return false; }
            lstg.TryAdd(i.Key, i.Value);
        }

        return titles.TryAdd(idx, new ValueModel { WordNumber = normalizeTitle.Value.words.Count, Simillars = lstg });
    }

    public static List<long>? Search(ref ConcurrentDictionary<long, ValueModel> titles, ref ConcurrentDictionary<int, ConcurrentDictionary<long, byte>> titleWords, string searchValue) {
        var normalizeTitle = Funcs.NormalizedTitle.GetBase(searchValue);
        if (normalizeTitle == null || !normalizeTitle.HasValue) return null;

        long idx = -1;

        foreach (var word in normalizeTitle.Value.words)
            Words.TryInWordDictionaryOrUpdate(word.Value);

        // Making "combinations of words" that we assume have a currently title

        var taws = new HashSet<int[]>(new SequenceEqualityComparer<int>());
        taws.Add(normalizeTitle.Value.words.Select(w => w.Key).ToArray());

        var newCombinations = new List<int[]>();

        foreach (var word in normalizeTitle.Value.words) {
            if (!Words.List.TryGetValue(word.Value, out var wordVal)) continue;

            foreach (var simWord in wordVal.Simillars.Where(sw => sw.Value >= 0.8)) {
                int hashCode = simWord.Key.GetHashCode();

                foreach (var combination in taws.ToList()) { // Создаем копию для безопасной итерации
                    if (!combination.Contains(word.Key)) continue;

                    var newCombination = combination.Select(k => k == word.Key ? hashCode : k).ToArray();
                    newCombinations.Add(newCombination);
                }
            }
        }

        // Adding new combinations after the iteration is completed

        foreach (var combination in newCombinations)
            taws.Add(combination);

        // Making combinations of titles that can include words from the "combinations of words"

        var artls = new HashSet<List<long>>();

        foreach(var i in taws) {
            var tl = new List<long>();
            foreach(var j in i)
                if(titleWords.TryGetValue(j, out var val)) {
                    var ly = val.Keys.OrderByDescending(i=>i).Where(k => k != idx).Take(1000);  // to test
                    tl.AddRange(ly);
                }

            if(tl.Count > 0) artls.Add(tl);
        }

        // We count how many times each "titles id" is mentioned in each combination

        var artlmtxs = new HashSet<Dictionary<long, int>>();

        foreach(var tl in artls) {
            var lstd = new Dictionary<long, int>();
            var tlor = tl.OrderBy(i=>i);

            long current = tlor.First();
            int count = 0;

            foreach(var i in tlor)
                if (i == current) count++;
                else {
                    lstd[current] = count;
                    current = i;
                    count = 1;
                }

            lstd[current] = count;

            artlmtxs.Add(lstd.OrderByDescending(i => i.Value).Take(1000).ToDictionary());
        }

        // We get rid of combinations and get a list of similar titles

        var artlmtxXs = new Dictionary<long, int>();
        foreach(var tl in artlmtxs)
            foreach(var v in tl)
                if(artlmtxXs.TryGetValue(v.Key, out var val)) {
                    if(val < v.Value)
                        val = v.Value;
                } else artlmtxXs.TryAdd(v.Key, v.Value);

        // We get rid of combinations and get a list of similar titles with coefficents
        
        var lsth = new Dictionary<long, double>();
        if(artlmtxXs.Count > 0)
            foreach (var i in artlmtxXs)
                if(titles.TryGetValue(i.Key, out var value))
                    lsth.TryAdd(i.Key, Funcs.GetMatchCoefficient(i.Value, normalizeTitle.Value.words.Count, value.WordNumber));

        

        // var taws = new HashSet<int[]>(new SequenceEqualityComparer<int>());
        // var newCombinations = new List<int[]>();
        // taws.Add(normalizeTitle.Value.words.Select(w => w.Key).ToArray());

        // foreach (var word in normalizeTitle.Value.words) {
        //     if (!Words.List.TryGetValue(word.Value, out var wordVal)) continue;

        //     foreach (var simWord in wordVal.Simillars.Where(sw => sw.Value > 0.8)) {
        //         int hashCode = simWord.Key.GetHashCode();

        //         foreach (var combination in taws.ToList()) { // Создаем копию для безопасной итерации
        //             if (!combination.Contains(word.Key)) continue;

        //             var newCombination = combination.Select(k => k == word.Key ? hashCode : k).ToArray();
        //             newCombinations.Add(newCombination);
        //         }
        //     }
        // }

        // var artlmtx = new Dictionary<long, int>();
        // foreach (var combination in taws)
        //     foreach (var wordId in combination)
        //         if (titleWords.TryGetValue(wordId, out var relatedArticles))
        //             foreach (var articleId in relatedArticles.Keys) 
        //                 artlmtx[articleId] = artlmtx.GetValueOrDefault(articleId) + 1;

        // var lsth = new Dictionary<long, double>();
        // foreach (var (articleId, count) in artlmtx)
        //     if (titles.TryGetValue(articleId, out var value))
        //         lsth[articleId] = Funcs.GetMatchCoefficient(count, normalizeTitle.Value.words.Count, value.WordNumber);

        return lsth.Where(l => l.Value > 0).OrderByDescending(l => l.Value).Select(l => l.Key).ToList();
    }

    class SequenceEqualityComparer<T> : IEqualityComparer<T[]> {
        public bool Equals(T[]? x, T[]? y) => x != null && y != null && x.SequenceEqual(y);

        public int GetHashCode(T[] obj) {
            unchecked {
                int hash = 17;
                foreach (var item in obj)
                    hash = hash * 31 + item.GetHashCode();
                return hash;
            }
        }
    }

    public class ValueModel {
        public int WordNumber { get; set; }
        public ConcurrentDictionary<long, double>? Simillars { get; set; }
    }
}

// namespace Alga.search;
// public class Titles {
//     /// <summary>
//     /// 
//     /// </summary>
//     /// <param name="titles"></param>
//     /// <param name="titleWords"> title (id / HashCode) list, where byte always is 0</param>
//     /// <param name="title"></param>
//     /// <param name="id"></param>
//     /// <param name="maxSimCoefToAdd"></param>
//     /// <returns></returns>
//     public static bool TryAddInTitleDictionary(ref ConcurrentDictionary<long, ValueModel> titles, ref ConcurrentDictionary<int, ConcurrentDictionary<long, byte>> titleWords, string title, long? id = null, double maxSimCoefToAdd = 1) {
//         var f = false;

//         var normalizeTitle = Funcs.NormalizedTitle.GetBase(title);

//         if(normalizeTitle == null || !normalizeTitle.HasValue) return f;

//         long idx = id ?? normalizeTitle.Value.title.GetHashCode();

//         foreach(var i in normalizeTitle.Value.words) {
//             Words.TryAddInWordDictionary(i.Value);

//             var wordList = titleWords.GetOrAdd(i.Key, _ => new ConcurrentDictionary<long, byte>());
//             wordList.TryAdd(idx, 0);
//         }

//         var taws = new HashSet<IEnumerable<int>>();
//         var wset = new HashSet<int>();
//         taws.Add(normalizeTitle.Value.words.Select(i => i.Key));
//         foreach(var i in normalizeTitle.Value.words) {
//             if(!wset.Add(i.Key)) continue;
//             if(!Words.List.TryGetValue(i.Value, out var wordVal)) continue;
//             if(wordVal.Simillars.Count == 0) continue;

//             var lq = taws.Where(x=>x.Contains(i.Key));

//             foreach(var word in wordVal.Simillars.Where(q => q.Value > 0.8)) {
//                 var hc = word.Key.GetHashCode();
//                 if(!wset.Add(hc)) continue;

//                 var tawssub = new HashSet<IEnumerable<int>>();
//                 var n = 200;

//                 foreach(var jx in lq) {
//                     if(n == 0) break;
//                     var hs = new HashSet<int>();
//                     foreach(var k in jx)
//                         if(k == i.Key) hs.Add(hc);
//                         else hs.Add(k);
//                     tawssub.Add(hs);
//                     n--;
//                 }

//                 if(tawssub.Count > 0)
//                     foreach(var tl in tawssub) taws.Add(tl);
//             }
//         }

//         var artls = new HashSet<List<long>>();
//         foreach(var i in taws) {
//             var tl = new List<long>();
//             foreach(var j in i)
//                 if(titleWords.TryGetValue(j, out var val))
//                     tl.AddRange(val.Keys.Where(k => k != idx));

//             if(tl.Count > 0)
//                 artls.Add(tl);
//         }

//         var artlmtxs = new HashSet<Dictionary<long, int>>();
//         foreach(var tl in artls) {
//             var lstd = new Dictionary<long, int>();
                
//             long current = tl[0];
//             int count = 0;

//             foreach (var i in tl.OrderBy(i=>i))
//                 if (i == current) count++;
//                 else {
//                     lstd[current] = count;
//                     current = i;
//                     count = 1;
//                 }

//             lstd[current] = count;

//             artlmtxs.Add(lstd);
//         }

//         var artlmtxXs = new Dictionary<long, int>();
//         foreach(var tl in artlmtxs)
//             foreach(var v in tl)
//                 if(artlmtxXs.TryGetValue(v.Key, out var val)) {
//                     if(val < v.Value)
//                         val = v.Value;
//                 } else artlmtxXs.TryAdd(v.Key, v.Value);

//         var lsth = new Dictionary<long, double>();
//         if(artlmtxXs.Count > 0)
//             foreach (var i in artlmtxXs)
//                 if(titles.TryGetValue(i.Key, out var value))
//                     lsth.TryAdd(i.Key, Funcs.GetMatchCoefficient(i.Value, normalizeTitle.Value.words.Count, value.WordNumber));

//         var lstg = new ConcurrentDictionary<long, double>();
//         foreach(var i in lsth.Where(i=>i.Value > 0.4).OrderByDescending(i=>i.Value)) {
//             if(i.Value > maxSimCoefToAdd) { f = false; break; }
//             lstg.TryAdd(i.Key, i.Value);
//         }

//         return titles.TryAdd(idx, new ValueModel { WordNumber = normalizeTitle.Value.words.Count, Simillars = lstg });;
//     }

//     public static List<long>? Search(ref ConcurrentDictionary<long, ValueModel> titles, ref ConcurrentDictionary<int, ConcurrentDictionary<long, byte>> titleWords, string searchValue) {
//         var normalizeTitle = Funcs.NormalizedTitle.GetBase(searchValue);

//         if(normalizeTitle == null || !normalizeTitle.HasValue) return null;

//         foreach(var i in normalizeTitle.Value.words)
//             Words.TryAddInWordDictionary(i.Value);

//         var taws = new HashSet<IEnumerable<int>>();
//         var wset = new HashSet<int>();
//         taws.Add(normalizeTitle.Value.words.Select(i => i.Key));
//         foreach(var i in normalizeTitle.Value.words) {
//             if(!wset.Add(i.Key)) continue;
//             if(!Words.List.TryGetValue(i.Value, out var wordVal)) continue;
//             if(wordVal.Simillars.Count == 0) continue;

//             var lq = taws.Where(x=>x.Contains(i.Key));

//             foreach(var word in wordVal.Simillars.Where(q => q.Value > 0.8)) {
//                 var hc = word.Key.GetHashCode();
//                 if(!wset.Add(hc)) continue;

//                 var tawssub = new HashSet<IEnumerable<int>>();
//                 var n = 200;

//                 foreach(var jx in lq) {
//                     if(n == 0) break;
//                     var hs = new HashSet<int>();
//                     foreach(var k in jx)
//                         if(k == i.Key) hs.Add(hc);
//                         else hs.Add(k);
//                     tawssub.Add(hs);
//                     n--;
//                 }

//                 if(tawssub.Count > 0)
//                     foreach(var tl in tawssub) taws.Add(tl);
//             }
//         }

//         var artls = new HashSet<List<long>>();
//         foreach(var i in taws) {
//             var tl = new List<long>();
//             foreach(var j in i)
//                 if(titleWords.TryGetValue(j, out var val))
//                     tl.AddRange(val.Keys);

//             if(tl.Count > 0)
//                 artls.Add(tl);
//         }

//         var artlmtxs = new HashSet<Dictionary<long, int>>();
//         foreach(var tl in artls) {
//             var lstd = new Dictionary<long, int>();
                
//             long current = tl[0];
//             int count = 0;

//             foreach (var i in tl.OrderBy(i=>i))
//                 if (i == current) count++;
//                 else {
//                     lstd[current] = count;
//                     current = i;
//                     count = 1;
//                 }

//             lstd[current] = count;

//             artlmtxs.Add(lstd);
//         }

//         var artlmtxXs = new Dictionary<long, int>();
//         foreach(var tl in artlmtxs)
//             foreach(var v in tl)
//                 if(artlmtxXs.TryGetValue(v.Key, out var val)) {
//                     if(val < v.Value)
//                         val = v.Value;
//                 } else artlmtxXs.TryAdd(v.Key, v.Value);

//         var lsth = new Dictionary<long, double>();
//         if(artlmtxXs.Count > 0)
//             foreach (var i in artlmtxXs)
//                 if(titles.TryGetValue(i.Key, out var value))
//                     lsth.TryAdd(i.Key, Funcs.GetMatchCoefficient(i.Value, normalizeTitle.Value.words.Count, value.WordNumber));

//         var lstg = new ConcurrentDictionary<long, double>();
//         foreach(var i in lsth.Where(i=>i.Value > 0).OrderByDescending(i=>i.Value)) {
//             // if(i.Value > maxSimCoefToAdd) { f = false; break; }
//             lstg.TryAdd(i.Key, i.Value);
//         }

//         return lstg.OrderByDescending(j=>j.Value).Select(i=>i.Key).ToList();
//     }

//     public class ValueModel {
//         /// <summary>
//         /// Word number in the title
//         /// </summary>
//         public int WordNumber { get; set; }
//         /// <summary>
//         /// Simillar articles - dictionary
//         /// Key: Article Id / Hashcode
//         /// Value: Artticle simillar coefficient
//         /// </summary>
//         public ConcurrentDictionary<long, double>? Simillars { get; set; }
//     }
// }