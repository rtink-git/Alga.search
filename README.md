# Alga.search
 
 The Alga.search nuget package - tools for searching among words and strings (titles). The purpose of this nuget package is to prepare your lists for quick searching





 ## How does this work?

 - 1. You send to nuget package a list(s) of titles (strings). Add to the lists as needed.
 - 2. The library (NuGet package) analyzes incoming titles (strings) during addition and stores only the data needed for fast search in the future.
 - 3. The search is ready for use. 





## How to use it? Step by step.

1. Open existing .NET Project

2. Add [Alga.search](https://www.nuget.org/packages/Alga.search) nuget package

4. You send a list which will be searched. 

- We analyze them and save only the necessary information about them in memory. 
- Library use thread-safe lists, so you can add data to them from different threads in parallel. 
- You can add lines from any part of the code without having to create an instance of the class.
- If you have several independent lists that you need to search, come up with unique identifiers for them and specify them when adding. ListId = 0 - is default.
- You can set the maximum number of titles (strings) - SetMaxRowNumber. All information about titles (strings) is stored in memory. Uncontrolled replenishment of new lines can cause data overflow in RAM. To calculate how much memory you are willing to allocate to store your lists, use the formula: N * 2KB
- For each word, save the ID of the list name in which the word appears - SetMaxSimilarTitlesInWord. We found the optimal maximum number of lines that can be stored in this list = 1000, the goal is to prevent memory overflow. But you can change this value
- Title id - unique identifier of title (string) but not required. If id is null we ourselves calculate the hash code of the title (string) and use it for id. We recommend using your external integer unique row identifier.

```
Parallel.ForEach(ArticlesFromDb, article => {
    Alga.search.Titles.TryAdd(title.title, title.id, 0);
});
```

BENCHMARK:

 - 1 loop + 10000 titles (to 0 title / comparison with 0 rows)          : 99 / 99 / 91 / 92 / 84 / 83 / 90 / 93 / 93 sec
 - 2 loop + 10000 titles (to 10000 title / comparison with 10000 rows)  : 170 / 152 / 154 / 154 / 147 / 196 sec
 - 3 loop + 10000 titles (to 20000 title / comparison with 20000 rows)  : 226 / 223 / 236 / 233 sec
 - 4 loop + 10000 titles (to 30000 title / comparison with 30000 rows)  : 240 sec
 - 5 loop + 10000 titles (to 40000 title / comparison with 40000 rows)  : 245 sec

5. Поиск по уникальному идентификатору. Полезен в проектах, когда необходимо найти похожие на этот заголовок заголовки (похожие публикации по названию - в сми)

```
var l = Alga.search.Titles.GetById(123); // where 123 is title id

var l = Alga.search.Titles.GetById(123, 1); // where 1 is list id,

var l = Alga.search.Titles.GetById(123, 1, 5); // where 5 is the number to return

var l = Alga.search.Titles.GetById(123, 1, 5, 0.3f); // where 0.3f is min similar coefficent

var l = Alga.search.Titles.GetById(123, 1, 5, 0.4f, 10); // where 5 is the number of minutes to cache for
```

BENCHMARK: 0.46 / 0.77 / 0.14 / 0.81 / 1.15 / 0.40 / 18.33 ms 

6. Поиск по строке или части строки. - класссический поиск по словам. Мы сравниваем слова поиска со словами которые есть в ваших заголовках, и выдаем результат списка id с коэфицентом схожести ваших заголовков с поисковым запросом.

```
var l = Alga.search.Titles.GetById("search query", 0, 30, 0.2f, 15); // where 123 is title id
```

BENCHMARK:

- 1 words in the search query, where 1 existing (in the list) word: 0.15 (0.022) / 0.30 (0.010) / 0.20 (0.009) / 0.40 (0.015) / 0.89 (0.020) ms
- 2 words in the search query, where 1 existing (in the list) word: 0.32 (0.008) / 0.14 (0.011) / 0.09 (0.009) / 0.15 (0.015) / 0.74 (0.016) ms
- 3 words in the search query, where 1 existing (in the list) word: 1.61 (0.008) / 1.79 (0.008) / 0.50 (0.006) / 1.18 (0.010) / 0.25 (0.009) ms
- 4 words in the search query, where 1 existing (in the list) word: 0.27 (0.008) / 0.17 (0.008) / 0.44 (0.008) / 0.32 (0.022) / 4.43 (0.007) ms
- 5 words in the search query, where 1 existing (in the list) word:

- 1 words in the search query, where 1 is not existing (in the list) word: 26.725 (0.016) / 185.49 (0.026) / 13.152 (0.073) / 9.8300 (0.372) ms
- 2 words in the search query, where 1 is not existing (in the list) word: 45.914 (0.019) / 96.812 (0.026) / 25.994 (0.207) / 17.587 (0.020)
- 3 words in the search query, where 2 is not existing (in the list) word: 175.67 / 268.28 / 232.19 / 133.49
- 4 words in the search query, where 2 is not existing (in the list) word: 232.04 / 93.937 / 217.33 / 236.27
- 5 words in the search query, where 4 is not existing (in the list) word: 462.27 / 338.75 / 298.32 / 541.93
- 5 words in the search query, where 2 is not existing (in the list) word: 318.90 / 286.28





## ADDITIONAL

Computer used for BENCHMARK: Computer for testing: MacBook Pro. 2,8 GHz 4‑core Intel Core i7





### UPDATES

What has been changed in new version (2.0.0) compared to the previous version (1.0.1)

 - Completely refactored the code.
 - Memory usage optimized.
 - Removed external lists; now using only internal ones.
 - Added logic to reduce the risk of memory overflow.
 - Removed the need for background code, considering it redundant for now.
 - Added settings for caching search results