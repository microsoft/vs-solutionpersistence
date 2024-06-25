// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.SolutionPersistence;

internal static class CollectionExtensions
{
#if NETFRAMEWORK
    public static bool TryAdd<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key, TValue value)
    {
        if (!dictionary.ContainsKey(key))
        {
            dictionary.Add(key, value);
            return true;
        }

        return false;
    }
#endif

    public static void AddIfNotNull<T>(this List<T> list, T? item)
    {
        if (item is not null)
        {
            list.Add(item);
        }
    }

    public static T AddAndReturn<T>(this List<T> list, T item)
    {
        list.Add(item);
        return item;
    }

    public static bool IsNullOrEmpty<T>([NotNullWhen(false)] this IReadOnlyCollection<T>? collection)
    {
        return collection is null || collection.Count == 0;
    }

    public static ReadOnlyListStructEnumerable<T> GetStructEnumerable<T>(this IReadOnlyList<T>? list) => new ReadOnlyListStructEnumerable<T>(list);

    public static ReadOnlyListStructReverseEnumerable<T> GetStructReverseEnumerable<T>(this IReadOnlyList<T>? list) => new ReadOnlyListStructReverseEnumerable<T>(list);

    /// <summary>
    /// Creates an array from a <see cref="IReadOnlyCollection{T}"/> with a selector to transform the items.
    /// </summary>
    /// <typeparam name="TSource">The item type of the input collection.</typeparam>
    /// <typeparam name="TResult">The item type of the new array.</typeparam>
    /// <param name="collection">The input collection.</param>
    /// <param name="selector">A way to convert TSource to TResult.</param>
    /// <returns>An array of the new items.</returns>
    public static List<TResult> ToList<TSource, TResult>(this IReadOnlyCollection<TSource> collection, Func<TSource, TResult> selector)
    {
        List<TResult> list = new List<TResult>(collection.Count);
        foreach (TSource item in collection)
        {
            list.Add(selector(item));
        }

        return list;
    }

    /// <summary>
    /// Creates an array from a <see cref="IReadOnlyCollection{T}"/> with a selector to transform the items.
    /// </summary>
    /// <typeparam name="TSource">The item type of the input collection.</typeparam>
    /// <typeparam name="TResult">The item type of the new array.</typeparam>
    /// <typeparam name="TState">The type of the state to pass to the predicate and selector.</typeparam>
    /// <param name="collection">The input collection.</param>
    /// <param name="predicate">A way to filter the items.</param>
    /// <param name="selector">A way to convert TSource to TResult.</param>
    /// <param name="state">The state to pass to the predicate and selector.</param>
    /// <returns>An array of the new items.</returns>
    public static List<TResult> WhereToList<TSource, TResult, TState>(
        this IReadOnlyCollection<TSource> collection,
        Func<TSource, TState, bool> predicate,
        Func<TSource, TState, TResult> selector,
        TState state)
    {
        List<TResult> list = new List<TResult>(collection.Count);
        foreach (TSource item in collection)
        {
            if (predicate(item, state))
            {
                list.Add(selector(item, state));
            }
        }

        return list;
    }
}

/// <summary>
/// Creates a enumerable struct wrapper around a list that might be null.
/// </summary>
internal readonly ref struct ListStructEnumerable<T>(List<T>? list)
{
    private static readonly List<T> EmptyList = [];

    public List<T>.Enumerator GetEnumerator() => (list ?? EmptyList).GetEnumerator();

    public int Count => list?.Count ?? 0;
}

internal readonly ref struct ReadOnlyListStructEnumerable<T>(IReadOnlyList<T>? list)
{
    public ReadOnlyListStructEnumerator<T> GetEnumerator() => new ReadOnlyListStructEnumerator<T>(list);
}

internal ref struct ReadOnlyListStructEnumerator<T>(IReadOnlyList<T>? list)
{
    private int index = -1;

    public readonly T Current => list![this.index];

    public bool MoveNext() => ++this.index < (list?.Count ?? 0);
}

internal readonly ref struct ReadOnlyListStructReverseEnumerable<T>(IReadOnlyList<T>? list)
{
    public ReadOnlyListStructReverseEnumerator<T> GetEnumerator() => new ReadOnlyListStructReverseEnumerator<T>(list);
}

internal ref struct ReadOnlyListStructReverseEnumerator<T>(IReadOnlyList<T>? list)
{
    private int index = list?.Count ?? 0;

    public readonly T Current => list![this.index];

    public bool MoveNext()
    {
        this.index--;
        return this.index >= 0;
    }
}
