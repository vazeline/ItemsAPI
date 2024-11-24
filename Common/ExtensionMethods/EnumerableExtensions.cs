using System.Collections.Generic;
using System.Linq;

namespace Common.ExtensionMethods
{
    public static class EnumerableExtensions
    {
        public static string StringJoin<T>(this IEnumerable<T> enumerable, char separator)
        {
            enumerable.ThrowIfNull();
            separator.ThrowIfDefault();

            return string.Join(separator, enumerable);
        }

        public static string StringJoin<T>(this IEnumerable<T> enumerable, string separator)
        {
            enumerable.ThrowIfNull();
            separator.ThrowIfNull();

            return string.Join(separator, enumerable);
        }

        public static bool ScrambledEquals<T>(this IEnumerable<T> list1, IEnumerable<T> list2)
        {
            var countMap = new Dictionary<T, int>();

            foreach (T s in list1)
            {
                if (countMap.TryGetValue(s, out int value))
                {
                    countMap[s] = ++value;
                }
                else
                {
                    countMap.Add(s, 1);
                }
            }

            foreach (T s in list2)
            {
                if (countMap.TryGetValue(s, out int value))
                {
                    countMap[s] = --value;
                }
                else
                {
                    return false;
                }
            }

            return countMap.Values.All(c => c == 0);
        }
    }
}
