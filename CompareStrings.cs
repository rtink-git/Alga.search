using System.Collections.Concurrent;

namespace Alga.search;
public class CompareStrings
{

    public string GetMaximumMatchString(string strOne, string strTwo)
    {
        int n = strOne.Length;
        int m = strTwo.Length;

        int[] previousRow = new int[m + 1];
        int[] currentRow = new int[m + 1];
        int maxLen = 0;
        int endIndex = 0; // Индекс окончания самой длинной подстроки в s1

        for (int i = 1; i <= n; i++) {
            for (int j = 1; j <= m; j++)
                if (strOne[i - 1] == strTwo[j - 1]) {
                    currentRow[j] = previousRow[j - 1] + 1;
                    if (currentRow[j] > maxLen) {
                        maxLen = currentRow[j];
                        endIndex = i; // Запоминаем конец текущей подстроки
                    }
                }
                else currentRow[j] = 0;

            // Смена строк
            var temp = previousRow;
            previousRow = currentRow;
            currentRow = temp;
        }

        // Формируем результат только один раз
        return maxLen > 0 ? strOne.Substring(endIndex - maxLen, maxLen) : string.Empty;
    }

    public ConcurrentDictionary<string, double> GetMatchCoefficientList(List<string> stringsListToCompare, List<string> stringDictionary) {
        var l = new ConcurrentDictionary<string, double>();

        Parallel.ForEach(stringsListToCompare, stringToCompare => {
            double maxCoefficient = 0;

            foreach (var dictionaryString in stringDictionary) {
                var matchString = GetMaximumMatchString(stringToCompare, dictionaryString);

                if (!string.IsNullOrEmpty(matchString)) {
                    double coefficient = Funcs.GetMatchCoefficient(matchString.Length, stringToCompare.Length, dictionaryString.Length);
                    if (coefficient > maxCoefficient)
                        maxCoefficient = coefficient;
                }
            }

            l[stringToCompare] = maxCoefficient;
        });

        return l;
    }



    // public double get_similarity_k_by_array(List<string> _stringToCompare, List<string> _stringToBeCompare)
    // {
    //     var stringToCompareDistinct = _stringToCompare.Distinct().ToList();
    //     var stringToBeCompareDistinct = _stringToBeCompare.Distinct().ToList();

    //     var gg = new List<Model>();
    //     var cache = new Dictionary<string, string>(); // Кэш для сравниваемых строк

    //     foreach (var strToCompare in stringToCompareDistinct) {
    //         Model? m = null;

    //         foreach (var strToBeCompareItem in stringToBeCompareDistinct) {
    //             string cacheKey = strToCompare + strToBeCompareItem;
    //             string sameStr = cache.ContainsKey(cacheKey) ? cache[cacheKey] : GetMaximumMatchString(strToCompare, strToBeCompareItem);
    //             cache[cacheKey] = sameStr; // Кэшируем результатcache[cacheKey] = sameStr; // Кэшируем результат

    //             double similarity = get_similarity_k(sameStr.Length, strToCompare.Length, strToBeCompareItem.Length);
    //             if (similarity > 0.65 && (m == null || m.percent < similarity))
    //                 m = new Model() { wrdIndex = stringToBeCompareDistinct.IndexOf(strToBeCompareItem) + 1, percent = similarity };
    //         }

    //         if (m != null) gg.Add(m);
    //     }

    //     int uniqueCount = gg.Select(g => g.wrdIndex).Distinct().Count(); // Количество уникальных индексов
    //     return (double)uniqueCount / stringToCompareDistinct.Count;
    // }

    // double get_matrix_strings_similarity(string _stringToCompare, string _stringToBeCompare)
    // {
    //     int mLength = _stringToBeCompare.Length;
    //     int nLength = _stringToCompare.Length;

    //     var matrix = new int[mLength, nLength];
    //     double maxLen = 0;

    //     for (int i = 0; i < mLength; i++)
    //         for (int j = 0; j < nLength; j++)
    //             if (_stringToBeCompare[i] == _stringToCompare[j])
    //             {
    //                 matrix[i, j] = (i > 0 && j > 0) ? matrix[i - 1, j - 1] + 1 : 1;
    //                 maxLen = Math.Max(maxLen, matrix[i, j]);
    //             }
    //             else matrix[i, j] = 0; // Сброс при несовпадении

    //     return maxLen < 3 ? 0 : maxLen / _stringToBeCompare.Length;
    // }

    // class Model
    // {
    //     public double wrdIndex { get; set; }
    //     public double percent { get; set; }
    // }
}
