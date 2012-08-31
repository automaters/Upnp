using System.Collections.Generic;
using System.Linq;
using System.Collections.Specialized;

namespace Upnp.Extensions
{
    /// <summary>
    /// Extension methods for Dictionary
    /// </summary>
    public static class Dictionary
    {

        /// <summary>
        /// Gets the value for the specified key or a default value.
        /// </summary>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <typeparam name="TValue">The type of the value.</typeparam>
        /// <param name="dictionary">The dictionary.</param>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        public static TValue ValueOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue value = default(TValue))
        {
            dictionary.TryGetValue(key, out value);
            return value;
        }

        public static string ValueOrDefault(this NameValueCollection collection, string key, string value = null)
        {
            // Find the current value and if it's either not null or the key exists return it
            // It's assumed that the indexer is a faster way of finding the key so it's used first
            string found = collection[key];
            if (found != null || collection.Keys.Cast<string>().Any(k => k == key))
                return found;

            // Otherwise return the default value
            return value;
        }

    }
}
