using System.Collections.Concurrent;
using System.Collections.Frozen;

namespace Alga.search;

public class Collections
{
    /// <summary>
    /// A dictionary storing basic information about words, keyed by the word itself.
    /// Key: word hash (long), 
    /// Value: word metadata structure.
    /// Average memory size for 100,000 rows (& 4 in Qgrams): 10 MB
    /// </summary>
    internal static readonly ConcurrentDictionary<long, Modules.WordInfo> WordsMap = new(concurrencyLevel: Environment.ProcessorCount, capacity: 10000); //new(concurrencyLevel: Environment.ProcessorCount, capacity: 1024);

    /// <summary>
    /// A dictionary of words' similarities, indexed by the word's hash code.A dictionary of words' similarities, indexed by the word's hash code.
    /// </summary>
    /// <remarks>
    /// The key is the word's hash code, and the value is a dictionary of similar words' hash codes and their similarity coefficients.
    /// The average size of each row is approximately ... bytes.
    /// </remarks>
    internal static ConcurrentDictionary<long, ConcurrentDictionary<long, float>> WordSimilarityMap { get; } = new(concurrencyLevel: Environment.ProcessorCount, capacity: 10000); //new(concurrencyLevel: Environment.ProcessorCount, capacity: 1024);

    /// <summary>
    /// Global mapping from word IDs to the set of title IDs in which each word appears
    /// Key: Word ID (long).
    /// Value: A thread-safe set of title IDs (ConcurrentDictionary<long, byte>) representing all titles containing the word.Value: A thread-safe set of title IDs (ConcurrentDictionary<long, byte>) representing all titles containing the word.
    /// Average memory size for 100,000 rows (& 1000 in HashSet): 1.3 GB
    /// </summary>
    internal static readonly ConcurrentDictionary<long, HashSet<Guid>> WordToTitlesMap = new(concurrencyLevel: Environment.ProcessorCount, capacity: 10000);
    internal static FrozenDictionary<long, Guid[]> WordToTitlesMapAsFrozen = FrozenDictionary<long, Guid[]>.Empty;


    /// <summary>
    /// Global mapping of title IDs to the sets of word identifiers they contain.
    /// Key: Unique title ID (long).
    /// Value: FrozenSet of word IDs (long), representing the distinct words extracted from the title.
    /// Average memory size for 100,000 rows: 17 MB.
    /// </summary>
    internal static readonly ConcurrentDictionary<Guid, long[]> TitlesWordMap = new(concurrencyLevel: Environment.ProcessorCount, capacity: 10000);

    internal static FrozenDictionary<Guid, long[]> TitlesWordMapAsFrozen = FrozenDictionary<Guid, long[]>.Empty;
}