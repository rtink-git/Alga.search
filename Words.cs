using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;


namespace Alga.search;
public class Words {
    readonly ILogger? Logger;
    public Words(ILogger? logger) => this.Logger = logger;

    /// <summary>
    /// Key - is word
    /// </summary>
    public static readonly ConcurrentDictionary<string, ValueModel> List = new ConcurrentDictionary<string, ValueModel>();

    public static bool TryAddInWordDictionary(string? word) {
        var f = false;

        if (string.IsNullOrWhiteSpace(word)) return f;

        word  = word.ToLower().Trim();

        // Попытка добавить новое слово
        var newValue = new ValueModel() {
            ActualCoefficient = 1,
            Simillars = GetMatchCoefficientList(word, CalculateMinCoefficient(word.Length))
        };

        if (List.TryAdd(word, newValue)) {
            f= true;

            foreach (var similarWord in newValue.Simillars)
                if (List.TryGetValue(similarWord.Key, out var similarValue))
                    similarValue?.Simillars.TryAdd(word, similarWord.Value);
        }
        else if (List.TryGetValue(word, out var existingValue)) {
            existingValue.AdditionDatetimes.TryAdd(DateTime.UtcNow, 0);

            if (existingValue.AdditionDatetimes.Count > 50 && existingValue.ActualCoefficientLastCheckDt < DateTime.UtcNow.AddHours(-1)) {
                var recentCount = existingValue.AdditionDatetimes.Count(date => date.Key > existingValue.ActualCoefficientLastCheckDt);

                if (recentCount > existingValue.ActualCoefficientLastCheckWordNumber) existingValue.ActualCoefficient = Math.Min(1.9, existingValue.ActualCoefficient + 0.1);
                else existingValue.ActualCoefficient = Math.Max(1.1, existingValue.ActualCoefficient - 0.1);

                existingValue.ActualCoefficientLastCheckWordNumber = recentCount;
                existingValue.ActualCoefficientLastCheckDt = DateTime.UtcNow;

                foreach(var i in existingValue.AdditionDatetimes.Where(date => date.Key < DateTime.UtcNow.AddHours(-3)))
                    existingValue.AdditionDatetimes.TryRemove(i.Key, out _);
            }
        }

        return f;
    }

    static double CalculateMinCoefficient(int length) => length switch { < 5 => 0.9, < 7 => 0.8, _ => 0.7 };

    static ConcurrentDictionary<string, double> GetMatchCoefficientList(string value, double minCoefficient = 0.7) {
        var l = new ConcurrentDictionary<string, double>();

        foreach (var dictionaryString in List) {
            var matchString = new CompareStrings().GetMaximumMatchString(value, dictionaryString.Key);

            if (string.IsNullOrEmpty(matchString)) continue;

            var coefficient = Alga.search.Funcs.GetMatchCoefficient(matchString.Length, value.Length, dictionaryString.Key.Length);
            if(coefficient >= minCoefficient && coefficient < 1)
                l.TryAdd(dictionaryString.Key, coefficient);
        }

        return l;
    }

    // Models

    public class ValueModel {
        public double ActualCoefficient {get; set; }
        public DateTime ActualCoefficientLastCheckDt {get; set; }
        public int ActualCoefficientLastCheckWordNumber {get; set; }
        public ConcurrentDictionary<DateTime, byte> AdditionDatetimes = new ConcurrentDictionary<DateTime, byte>();
        public ConcurrentDictionary<string, double> Simillars {get; set; } = new ConcurrentDictionary<string, double>();
    }
}