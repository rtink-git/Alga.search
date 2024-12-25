using System.Text;
using System.Collections.Concurrent;

namespace Alga.search;

public static partial class Funcs {
public static class NormalizedTitle {

    public static (string title, Dictionary<int, string> words)? GetBase(string? title) {
        if (string.IsNullOrWhiteSpace(title)) return null;

        var normTitle = new StringBuilder();
        var wordSet = new HashSet<string>(); // Для удаления дубликатов
        var dl = new Dictionary<int, string>();

        foreach (var word in title!.ToLower().Split(new[] { ' ', ',', '.', '!', '?', ':', ';', '-', '_', '/', '\\', '|', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)) {
            if (word.Length > 0 && wordSet.Add(word)) {
                dl[word.GetHashCode()] = word;
                normTitle.Append(word).Append(' ');
            }
        }

        if (wordSet.Count == 0) return null;

        return (normTitle.ToString().Trim(), dl);
    }
}
}
