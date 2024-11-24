using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.ExtensionMethods
{
    public static class DictionaryExtensions
    {
        /// <summary>
        /// This is NOT thread-safe, use only when you're confident you're on a single thread.
        /// </summary>
        public static TValue GetOrAdd<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue valueToAddIfNotExists)
        {
            if (!dictionary.TryGetValue(key, out TValue value))
            {
                value = valueToAddIfNotExists;
                dictionary.Add(key, value);
                return value;
            }

            return value;
        }

        /// <summary>
        /// This is NOT thread-safe, use only when you're confident you're on a single thread.
        /// </summary>
        public static TValue GetOrAdd<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, Func<TValue> valueToAddIfNotExists)
        {
            if (!dictionary.TryGetValue(key, out TValue value))
            {
                value = valueToAddIfNotExists();
                dictionary.Add(key, value);
                return value;
            }

            return value;
        }

        public static TValue GetOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key)
        {
            if (dictionary.TryGetValue(key, out var value))
            {
                return value;
            }

            return default;
        }
    }
}
