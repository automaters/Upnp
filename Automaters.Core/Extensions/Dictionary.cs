using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Automaters.Core.Extensions
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

    }
}
