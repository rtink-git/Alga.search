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

    public static ValueModel? TryInWordDictionaryOrUpdate(string? word) {
        if (string.IsNullOrWhiteSpace(word)) return null;

        word  = word.ToLower().Trim();

        var dtn = DateTime.UtcNow;

        var newValue = new ValueModel() {
            ActualCoefficient = 1,
            ActualCoefficientLastCheckDt = dtn,
            ActualCoefficientLastCheckWordNumber = 1,
            Simillars = GetMatchCoefficientList(word, CalculateMinCoefficient(word.Length)),
            AdditionDatetimes = new ConcurrentDictionary<DateTime, byte>() { [dtn]=0 } 
        };

        if (List.TryAdd(word, newValue)) {
            foreach (var similarWord in newValue.Simillars)
                if (List.TryGetValue(similarWord.Key, out var similarValue))
                    similarValue?.Simillars.TryAdd(word, similarWord.Value);

            return newValue;
        }
        else if (List.TryGetValue(word, out var existingValue))
            if (existingValue.ActualCoefficientLastCheckDt < dtn.AddHours(-1)) {
                var recentCount = existingValue.AdditionDatetimes.Count(date => date.Key > existingValue.ActualCoefficientLastCheckDt);

                existingValue.ActualCoefficient = (recentCount > existingValue.ActualCoefficientLastCheckWordNumber) ? Math.Min(1.9, existingValue.ActualCoefficient + 0.1) : existingValue.ActualCoefficient = Math.Max(1, existingValue.ActualCoefficient - 0.1);
                existingValue.ActualCoefficientLastCheckWordNumber = recentCount;
                existingValue.ActualCoefficientLastCheckDt = dtn;

                return existingValue;
            }

        return null;
    }

    static double CalculateMinCoefficient(int length) => length switch { < 5 => 0.9, < 7 => 0.8, _ => 0.7 };

    static ConcurrentDictionary<string, double> GetMatchCoefficientList(string value, double minCoefficient = 0.7) {
        var l = new ConcurrentDictionary<string, double>();

        foreach (var dictionaryString in List) {
            var matchString = new CompareStrings().GetMaximumMatchString(value, dictionaryString.Key);

            if (string.IsNullOrEmpty(matchString)) continue;

            var coefficient = Funcs.GetMatchCoefficient(matchString.Length, value.Length, dictionaryString.Key.Length);
            if(coefficient >= minCoefficient && coefficient < 1)
                l.TryAdd(dictionaryString.Key, coefficient);
        }

        return l;
    }

    internal static void DeleteOutdate() {
        var outdateL = new HashSet<string>();
        var dtdic = new Dictionary<string, DateTime>();
        var simdis = new Dictionary<string, string>();

        var dtnow = DateTime.UtcNow;

        var qdt = dtnow.AddHours(-3);

        foreach (var i in List) {
            var mxdt = i.Value.AdditionDatetimes.Max(i=>i.Key);

            if (mxdt < dtnow.AddHours(-6)) outdateL.Add(i.Key);

            foreach(var j in i.Value.AdditionDatetimes) if(j.Key < qdt) dtdic.Add(i.Key, j.Key);

            foreach(var j in i.Value.Simillars.OrderByDescending(i=>i.Value).Skip(100)) simdis.Add(i.Key, j.Key);
        }

        foreach(var i in dtdic) List[i.Key].AdditionDatetimes.TryRemove(i.Value, out _);

        foreach(var i in simdis) List[i.Key].Simillars.TryRemove(i.Value, out _);

        foreach (var i in outdateL) List.TryRemove(i, out _);
    }

    // Models
    public class ValueModel {
        public double ActualCoefficient {get; set; }
        public DateTime ActualCoefficientLastCheckDt {get; set; }
        public int ActualCoefficientLastCheckWordNumber {get; set; }
        public ConcurrentDictionary<DateTime, byte> AdditionDatetimes { get; set; } = new ConcurrentDictionary<DateTime, byte>();
        public ConcurrentDictionary<string, double> Simillars {get; set; } = new ConcurrentDictionary<string, double>();
    }
}