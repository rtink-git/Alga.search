# Alga.search

The Alga.search nuget package - tools for searching among words and strings (titles). The purpose of this nuget package is to prepare your lists for quick searching

## How does this work?

1.  You send to nuget package a list(s) of titles (strings). Add to the lists as needed.
2.  The library (NuGet package) analyzes incoming titles (strings) during addition and stores only the data needed for fast search in the future.
3.  The search is ready for use.

## How to use it? Step by step.

1. Open existing .NET Project

2. Add [Alga.search](https://www.nuget.org/packages/Alga.search) nuget package

3. You send a list which will be searched.

- We analyze them and save only the necessary information about them in memory.
- Library use thread-safe lists, so you can add data to them from different threads in parallel.
- You can add lines from any part of the code without having to create an instance of the class.
- If you have several independent lists that you need to search, come up with unique identifiers for them and specify them when adding. ListId = 0 - is default.
- You can set the maximum number of titles (strings) - SetMaxRowNumber. All information about titles (strings) is stored in memory. Uncontrolled replenishment of new lines can cause data overflow in RAM. To calculate how much memory you are willing to allocate to store your lists, use the formula: N \* 2KB
- For each word, save the ID of the list name in which the word appears - SetMaxSimilarTitlesInWord. We found the optimal maximum number of lines that can be stored in this list = 1000, the goal is to prevent memory overflow. But you can change this value
- Title id - unique identifier of title (string) but not required. If id is null we ourselves calculate the hash code of the title (string) and use it for id. We recommend using your external integer unique row identifier.

```
Parallel.ForEach(ArticlesFromDb, article => {
    Alga.search.Titles.TryAdd(title.title, title.id, 0);
});
```

BENCHMARK:

- 1 loop + 10000 titles (to 0 title / comparison with 0 rows) : v2.3.0: 18 sec v2.2.0: 46 (minHash) v2.0.0: 99 (LCS) sec
- 2 loop + 10000 titles (to 10000 title / comparison with 10000 rows) : v2.3.0: 26 sec v2.2.0: 95 sec (minHash) v2.0.0: 154 (LCS) sec
- 3 loop + 10000 titles (to 20000 title / comparison with 20000 rows) : v2.3.0: v2.2.0: 119 sec (minHash) v2.0.0: 223 (LCS) sec
- 4 loop + 10000 titles (to 30000 title / comparison with 30000 rows) : v2.3.0: v2.2.0: 134 (minHash) v2.0.0: 295 (LCS) sec
- 5 loop + 10000 titles (to 40000 title / comparison with 40000 rows) : v2.3.0: v2.2.0: 157 sec (minHash) v2.0.0: 245 (LCS) sec

4. Search by unique identifier. Useful in projects where it is necessary to find titles similar to a given one (e.g., related articles by title in media publications).

```
var l = Alga.search.Titles.SeaarchSimilarTitlesById(123); // where 123 is title id

var l = Alga.search.Titles.GetSimilarTitlesById(123, 5); // where 1 is list id,

var l = Alga.search.Titles.GetSimilarTitlesById(123, 5, 0.3f); // where 0.3f is min similar coefficent

```

BENCHMARK: 0.018 / 0.025 / 0.061 / 0.034 / 0.019 / 0.15 ms

5. Search by full or partial string – a classic word-based search. We compare the search words with the words present in your titles and return a list of IDs with a similarity coefficient indicating how closely your titles match the search query.

```
var l = Alga.search.Titles.SearchByString("search query", 30, 0.2f);
```

BENCHMARK:

- 1 words in the search query, where 1 word: 1.6 ms
- 2 words in the search query, where 2 word: 1.3 ms
- 3 words in the search query, where 3 word: 3.0 ms
- 4 words in the search query, where 4 word: 3.4 ms
- 5 words in the search query, where 5 word: 3.2 ms

## ADDITIONAL

Computer used for BENCHMARK: Computer for testing: MacBook Pro. 2,8 GHz 4‑core Intel Core i7

### UPDATES

What has been changed in new version (3.0.0) compared to the previous version (2.3.0)

- Consumes less RAM memory
- Speeded up search queries
- Title identifiers now use Guid type instead of long.
