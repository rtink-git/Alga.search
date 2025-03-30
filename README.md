# Alga.search
 
 The Alga.search nuget package - tools for searching among words and strings (titles). The purpose of this nuget package is to prepare your lists for quick searching





 ## How does this work?
 1. You send to nuget package a list(s) of titles (strings). Add to the lists as needed.
 2. The library (NuGet package) analyzes incoming titles (strings) during addition and stores only the data needed for fast search in the future.
 3. The search is ready for use. 





## How to use it? Step by step.

1. Open existing .NET Project

2. Add [Alga.search](https://www.nuget.org/packages/Alga.search) nuget package

3. You send a list which will be searched. 

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

 - 1 loop + 10000 titles (to 0 title / comparison with 0 rows)          : v2.2.0: 55 / 67 / 80 / 59 / 50 sec (minHash)          v2.0.0: 99 / 91 / 92 (LCS) sec
 - 2 loop + 10000 titles (to 10000 title / comparison with 10000 rows)  : v2.2.0: 95 / 104 / 132 / 99 / 96 / 94 sec (minHash)   v2.0.0: 154 / 147 / 191 (LCS) sec
 - 3 loop + 10000 titles (to 20000 title / comparison with 20000 rows)  : v2.2.0: 110 / 115 / 170 / 132 / 105 sec (minHash)     v2.0.0: 223 / 236 / 233 (LCS) sec
 - 4 loop + 10000 titles (to 30000 title / comparison with 30000 rows)  : v2.2.0: 138 / 150 / 153 / 160 / 128 sec (minHash)     v2.0.0: 295 / 240 (LCS) sec
 - 5 loop + 10000 titles (to 40000 title / comparison with 40000 rows)  : v2.2.0: 150 / 176 / 164 / 149 / 133 sec (minHash)     v2.0.0: 245 (LCS) sec

4. Поиск по уникальному идентификатору. Полезен в проектах, когда необходимо найти похожие на этот заголовок заголовки (похожие публикации по названию - в сми)

```
var l = Alga.search.Titles.GetById(123); // where 123 is title id

var l = Alga.search.Titles.GetById(123, 1); // where 1 is list id,

var l = Alga.search.Titles.GetById(123, 1, 5); // where 5 is the number to return

var l = Alga.search.Titles.GetById(123, 1, 5, 0.3f); // where 0.3f is min similar coefficent

var l = Alga.search.Titles.GetById(123, 1, 5, 0.4f, 10); // where 5 is the number of minutes to cache for
```

BENCHMARK: 0.15 / 0.27 / 0.80 / 0.10 / 0.57 / 1.00 / 0.46 / 0.77 / 0.14 ms 

5. Поиск по строке или части строки. - класссический поиск по словам. Мы сравниваем слова поиска со словами которые есть в ваших заголовках, и выдаем результат списка id с коэфицентом схожести ваших заголовков с поисковым запросом.

```
var l = Alga.search.Titles.GetById("search query", 0, 30, 0.2f, 15);
```

BENCHMARK:

- 1 words in the search query, where 1 existing (in the list) word: 0.22 / 0.25 / 0.37 / 0.23 ms
- 2 words in the search query, where 2 existing (in the list) word: 0.23 / 0.50 / 0.15 / 0.25 ms
- 3 words in the search query, where 3 existing (in the list) word: 0.50 / 0.40 / 0.30 / 1.61 ms
- 4 words in the search query, where 4 existing (in the list) word: 0.70 / 1.50 / 2.00 / 1.00 ms
- 5 words in the search query, where 5 existing (in the list) word: 1.20 / 0.70 / 0.45 / 0.40 ms

- 1 words in the search query, where 1 is not existing (in the list) word: 41.00 / 45.00 / 64.00 ms
- 2 words in the search query, where 2 is not existing (in the list) word: 91.00 / 79.00 / 79.91 ms
- 3 words in the search query, where 3 is not existing (in the list) word: 210.0 / 202.0 / 175.6 ms
- 4 words in the search query, where 4 is not existing (in the list) word: 335.0 / 320.0 / 298.0 ms
- 5 words in the search query, where 5 is not existing (in the list) word: 225.0 / 270.0 / 462.2 ms





## ADDITIONAL

Computer used for BENCHMARK: Computer for testing: MacBook Pro. 2,8 GHz 4‑core Intel Core i7





### UPDATES

What has been changed in new version (2.2.0) compared to the previous version (2.0.0)

 - Word comprassion aalgorithm was changed from LCS to MinHash. Line adding speed increaseed by 40%