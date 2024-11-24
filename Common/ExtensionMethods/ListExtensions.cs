using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Common.Utility;

namespace Common.ExtensionMethods
{
    public static class ListExtensions
    {
        public static void DiffByIdWith<TSource, TDiff>(
            this IList<TSource> source,
            IList<TDiff> diffWith,
            Action<TDiff> onNewMember = null,
            Action<TDiff, TSource> onUpdatedMember = null,
            Action<TSource> onDeletedMember = null,
            Func<TSource, int> sourceIdSelector = null,
            Func<TDiff, int> diffWithIdSelector = null,
            bool assumeDefaultIdsInDiffWithListAreNew = true)
        {
            (sourceIdSelector, diffWithIdSelector) = PerformDiff(
                source: source,
                diffWith: diffWith,
                sourceIdSelector: sourceIdSelector,
                diffWithIdSelector: diffWithIdSelector,
                assumeDefaultIdsInDiffWithListAreNew: assumeDefaultIdsInDiffWithListAreNew,
                newMembers: out var newMembers,
                sourceIdMap: out var sourceIdMap,
                diffWithIdMap: out var diffWithIdMap);

            if (onNewMember != null)
            {
                foreach (var value in newMembers)
                {
                    onNewMember(value);
                }

                foreach (var (key, value) in diffWithIdMap.Where(x => !sourceIdMap.ContainsKey(x.Key)))
                {
                    onNewMember(value);
                }
            }

            if (onUpdatedMember != null)
            {
                foreach (var (key, value) in diffWithIdMap.Where(x => sourceIdMap.ContainsKey(x.Key)))
                {
                    onUpdatedMember(value, sourceIdMap[key]);
                }
            }

            if (onDeletedMember != null)
            {
                foreach (var (key, value) in sourceIdMap.Where(x => !diffWithIdMap.ContainsKey(x.Key)))
                {
                    onDeletedMember(value);
                }
            }
        }

        public static void DiffByIdWith<TSource, TDiff>(
            this IList<TSource> source,
            IList<TDiff> diffWith,
            Action<TDiff, CancellationTokenSource> onNewMember = null,
            Action<TDiff, TSource, CancellationTokenSource> onUpdatedMember = null,
            Action<TSource, CancellationTokenSource> onDeletedMember = null,
            Func<TSource, int> sourceIdSelector = null,
            Func<TDiff, int> diffWithIdSelector = null,
            bool assumeDefaultIdsInDiffWithListAreNew = true)
        {
            (sourceIdSelector, diffWithIdSelector) = PerformDiff(
                source: source,
                diffWith: diffWith,
                sourceIdSelector: sourceIdSelector,
                diffWithIdSelector: diffWithIdSelector,
                assumeDefaultIdsInDiffWithListAreNew: assumeDefaultIdsInDiffWithListAreNew,
                newMembers: out var newMembers,
                sourceIdMap: out var sourceIdMap,
                diffWithIdMap: out var diffWithIdMap);

            var cancellationTokenSource = new CancellationTokenSource();

            if (onNewMember != null)
            {
                foreach (var value in newMembers)
                {
                    onNewMember(value, cancellationTokenSource);

                    if (cancellationTokenSource.Token.IsCancellationRequested)
                    {
                        return;
                    }
                }

                foreach (var (key, value) in diffWithIdMap.Where(x => !sourceIdMap.ContainsKey(x.Key)))
                {
                    onNewMember(value, cancellationTokenSource);

                    if (cancellationTokenSource.Token.IsCancellationRequested)
                    {
                        return;
                    }
                }
            }

            if (onUpdatedMember != null)
            {
                foreach (var (key, value) in diffWithIdMap.Where(x => sourceIdMap.ContainsKey(x.Key)))
                {
                    onUpdatedMember(value, sourceIdMap[key], cancellationTokenSource);

                    if (cancellationTokenSource.Token.IsCancellationRequested)
                    {
                        return;
                    }
                }
            }

            if (onDeletedMember != null)
            {
                foreach (var (key, value) in sourceIdMap.Where(x => !diffWithIdMap.ContainsKey(x.Key)))
                {
                    onDeletedMember(value, cancellationTokenSource);

                    if (cancellationTokenSource.Token.IsCancellationRequested)
                    {
                        return;
                    }
                }
            }
        }

        public static async Task DiffByIdWithAsync<TSource, TDiff>(
            this IList<TSource> source,
            IList<TDiff> diffWith,
            Func<TDiff, CancellationTokenSource, Task> onNewMemberAsync = null,
            Func<TDiff, TSource, CancellationTokenSource, Task> onUpdatedMemberAsync = null,
            Func<TSource, CancellationTokenSource, Task> onDeletedMemberAsync = null,
            Func<TSource, int> sourceIdSelector = null,
            Func<TDiff, int> diffWithIdSelector = null,
            bool assumeDefaultIdsInDiffWithListAreNew = true)
        {
            (sourceIdSelector, diffWithIdSelector) = PerformDiff(
                source: source,
                diffWith: diffWith,
                sourceIdSelector: sourceIdSelector,
                diffWithIdSelector: diffWithIdSelector,
                assumeDefaultIdsInDiffWithListAreNew: assumeDefaultIdsInDiffWithListAreNew,
                newMembers: out var newMembers,
                sourceIdMap: out var sourceIdMap,
                diffWithIdMap: out var diffWithIdMap);

            var cancellationTokenSource = new CancellationTokenSource();

            if (onNewMemberAsync != null)
            {
                foreach (var value in newMembers)
                {
                    await onNewMemberAsync(value, cancellationTokenSource);

                    if (cancellationTokenSource.Token.IsCancellationRequested)
                    {
                        return;
                    }
                }
            }

            if (onUpdatedMemberAsync != null)
            {
                foreach (var (key, value) in diffWithIdMap.Where(x => sourceIdMap.ContainsKey(x.Key)))
                {
                    await onUpdatedMemberAsync(value, sourceIdMap[key], cancellationTokenSource);

                    if (cancellationTokenSource.Token.IsCancellationRequested)
                    {
                        return;
                    }
                }
            }

            if (onDeletedMemberAsync != null)
            {
                foreach (var (key, value) in sourceIdMap.Where(x => !diffWithIdMap.ContainsKey(x.Key)))
                {
                    await onDeletedMemberAsync(value, cancellationTokenSource);

                    if (cancellationTokenSource.Token.IsCancellationRequested)
                    {
                        return;
                    }
                }
            }
        }

        public static void ForEach<T>(this IReadOnlyList<T> list, Action<T> action)
        {
            action.ThrowIfNull();

            foreach (var item in list)
            {
                action(item);
            }
        }

        private static (Func<TSource, int> SourceIdSelector, Func<TDiff, int> DiffWithIdSelector) PerformDiff<TSource, TDiff>(
            IList<TSource> source,
            IList<TDiff> diffWith,
            Func<TSource, int> sourceIdSelector,
            Func<TDiff, int> diffWithIdSelector,
            bool assumeDefaultIdsInDiffWithListAreNew,
            out List<TDiff> newMembers,
            out Dictionary<int, TSource> sourceIdMap,
            out Dictionary<int, TDiff> diffWithIdMap)
        {
            if (sourceIdSelector == null)
            {
                var sourceIdProp = typeof(TSource).GetProperty("Id");

                if (sourceIdProp == null || sourceIdProp.PropertyType != typeof(int))
                {
                    throw new Exception("Source list type doesn't contain an int Id property");
                }

                sourceIdSelector = x => (int)sourceIdProp.GetValue(x);
            }

            if (diffWithIdSelector == null)
            {
                var diffWithIdProp = typeof(TDiff).GetProperty("Id");

                if (diffWithIdProp == null || diffWithIdProp.PropertyType != typeof(int))
                {
                    throw new Exception("DiffWith list type doesn't contain an int Id property");
                }

                diffWithIdSelector = x => (int)diffWithIdProp.GetValue(x);
            }

            var diffWithInternal = diffWith.ToList(); // make a copy of diffWith to avoid modifying the list passed by ref into this method
            newMembers = new List<TDiff>();

            if (assumeDefaultIdsInDiffWithListAreNew)
            {
                for (var i = 0; i < diffWithInternal.Count; i++)
                {
                    var diffWithMemberId = diffWithIdSelector(diffWithInternal[i]);

                    if (diffWithMemberId == default)
                    {
                        newMembers.Add(diffWithInternal[i]);
                        diffWithInternal.RemoveAt(i);
                        i--;
                    }
                }
            }

            sourceIdMap = source.ToDictionary(x => sourceIdSelector(x), x => x);
            diffWithIdMap = diffWithInternal.ToDictionary(x => diffWithIdSelector(x), x => x);

            return (sourceIdSelector, diffWithIdSelector);
        }
    }
}
