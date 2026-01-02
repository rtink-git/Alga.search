using System.Collections.Concurrent;
using System.Collections.Frozen;

namespace Alga.search;

internal class Collections
{
    /// <summary>
    /// A temporary collection of titles awaiting processing.
    /// Titles added here are later processed and integrated into the main collections.
    /// </summary>
    internal static readonly ConcurrentDictionary<Guid, string> TitlesBufferMap = new();

    /// <summary>
    /// A dictionary storing basic information about words, keyed by the word itself.
    /// Key: word hash (long), 
    /// Value: word metadata structure.
    /// </summary>
    internal static FrozenDictionary<long, Models.WordInfo> WordsMap = FrozenDictionary<long, Models.WordInfo>.Empty; //new(concurrencyLevel: Environment.ProcessorCount, capacity: 1024);

    /// <summary>
    /// A dictionary of words' similarities, indexed by the word's hash code.
    /// Key: Word hash code
    /// Value Key: The key is the word's hash code, and the value is a dictionary of similar words' hash codes and their similarity coefficients.
    /// Value Value: The average size of each row is approximately ... bytes.
    /// </remarks>
    internal static FrozenDictionary<long, FrozenDictionary<long, float>> WordSimilarityMap = FrozenDictionary<long, FrozenDictionary<long, float>>.Empty; //new(concurrencyLevel: Environment.ProcessorCount, capacity: 1024);

    /// <summary>
    /// Global mapping from word IDs to the set of title IDs in which each word appears
    /// Key: Word ID (long).
    /// Value: A thread-safe set of title IDs array representing all titles containing the word.Value: A thread-safe set of title IDs (ConcurrentDictionary<long, byte>) representing all titles containing the word.
    /// </summary>
    internal static FrozenDictionary<long, Guid[]> WordToTitlesMap = FrozenDictionary<long, Guid[]>.Empty;

    /// <summary>
    /// Global mapping of title IDs to the sets of word identifiers they contain.
    /// Key: Unique title ID (long).
    /// Value: FrozenSet of word IDs (long), representing the distinct words extracted from the title.
    /// Average memory size for 100,000 rows: 17 MB.
    /// </summary>
    internal static FrozenDictionary<Guid, long[]> TitlesWordMap = FrozenDictionary<Guid, long[]>.Empty;
}