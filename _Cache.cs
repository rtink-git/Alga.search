using Microsoft.Extensions.Caching.Memory;

namespace Alga.search;
/// <summary>
/// The _Cache class provides methods for working with in-memory caching
/// It is used to store and retrieve cached data efficiently.
/// </summary>
internal static class _Cache {
    /// <summary>
    /// An in-memory cache object with default settings
    /// </summary>
    static readonly MemoryCache Memory = new(new MemoryCacheOptions());

    /// <summary>
    /// Represents a caching session for storing and retrieving search results.
    /// </summary>
    public class Session {
        /// <summary>
        /// A cached list of long values, retrieved from memory if available.
        /// </summary>
        public List<long>? ReturnList;
        /// <summary>
        /// The cache expiration time in minutes.
        /// </summary>
        int _CacheInMin;
        /// <summary>
        /// A unique cache key for identifying stored data.
        /// </summary>
        string _Key;

        /// <summary>
        /// Initializes a new caching session with the specified parameters.
        /// </summary>
        /// <param name="value">The main value for generating the cache key. It can be a query string or an id</param>
        /// <param name="listId">The list (of titles (strings)) identifier (default is 0)</param>
        /// <param name="take"></param>
        /// <param name="cacheInMin"></param>
        public Session(string value, int listId=0, int take=int.MaxValue, int cacheInMin=0) { 
            _CacheInMin=cacheInMin; 

            _Key = $"Search_{value}_{take}_{listId}"; // Generates a unique cache key based on the input parameters.

            ReturnList = cacheInMin > 0 && Memory.TryGetValue(_Key, out List<long>? cachedResult) ? cachedResult : null;
        }

        /// <summary>
        /// Stores a list of long values in the cache with the specified expiration time.
        /// </summary>
        /// <param name="result">The list of long values to store in the cache.</param>
        public void Set(List<long>? result) {
            if (_CacheInMin > 0 && result is { Count: > 0 })
                Memory.Set(_Key, result, new MemoryCacheEntryOptions {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(_CacheInMin)
            });
        }
    }
}