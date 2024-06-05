// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Linq;

namespace Microsoft.VisualStudio.SolutionPersistence.Utilities;

/// <summary>
/// Provides some dictionary like functionality with a list of key value pairs.
/// Used for small collections where the overhead of a dictionary is too high.
/// </summary>
/// <typeparam name="TKey">Key type.</typeparam>
/// <typeparam name="TValue">Value type.</typeparam>
internal readonly struct Lictionary<TKey, TValue> : IReadOnlyDictionary<TKey, TValue>
    where TKey : notnull
{
    private static readonly EntryKeyComparer DefaultComparer = new EntryKeyComparer(Comparer<TKey>.Default);

    private readonly List<KeyValuePair<TKey, TValue>> items;
    private readonly EntryKeyComparer comparer;

    public Lictionary()
        : this(capacity: 0, comparer: null)
    {
    }

    public Lictionary(int capacity, IComparer<TKey>? comparer = null)
    {
        this.comparer = comparer is null ? DefaultComparer : new EntryKeyComparer(comparer);
        this.items = new List<KeyValuePair<TKey, TValue>>(capacity);
    }

    public Lictionary(IReadOnlyCollection<KeyValuePair<TKey, TValue>> values, IComparer<TKey>? comparer = null)
    {
        this.comparer = comparer is null ? DefaultComparer : new EntryKeyComparer(comparer);
        this.items = [.. values];
        this.items.Sort(this.comparer);

        if (default(TKey) is null && this.ContainsKey(default!))
        {
            throw new ArgumentNullException(nameof(values));
        }

        KeyValuePair<TKey, TValue> lastEntry = default;
        foreach (KeyValuePair<TKey, TValue> entry in this.items)
        {
            if (this.comparer.Equals(lastEntry, entry))
            {
                throw new ArgumentException("Duplicate property name " + lastEntry.Key, nameof(values));
            }

            lastEntry = entry;
        }
    }

    public void Add(TKey key, TValue value)
    {
        if (!this.TryAdd(key, value))
        {
            throw new ArgumentException("Duplicate property name has " + key, nameof(key));
        }
    }

    internal bool TryAdd(TKey key, TValue value)
    {
        int index = this.BinarySearch(key);
        if (index >= 0)
        {
            return false;
        }
        else
        {
            this.items.Insert(~index, new KeyValuePair<TKey, TValue>(key, value));
            return true;
        }
    }

    public bool Remove(TKey key)
    {
        int index = this.BinarySearch(key);
        if (index >= 0)
        {
            this.items.RemoveAt(index);
            return true;
        }

        return false;
    }

    public void Clear() => this.items.Clear();

    public TValue this[TKey key]
    {
        get => this.TryGetValue(key, out TValue? value) ? value : throw new ArgumentOutOfRangeException(nameof(key));
        set
        {
            int index = this.BinarySearch(key);
            if (index >= 0)
            {
                this.items[index] = new(key, value);
            }
            else
            {
                this.items.Insert(~index, new(key, value));
            }
        }
    }

    public IEnumerable<TKey> Keys => this.Select(x => x.Key);

    public IEnumerable<TValue> Values => this.Select(x => x.Value);

    public int Count => this.items.Count;

    public bool ContainsKey(TKey key) => this.BinarySearch(key) >= 0;

#if NETFRAMEWORK
#nullable disable warnings
#endif
    public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
#if NETFRAMEWORK
#nullable restore
#endif
    {
        int index = this.BinarySearch(key);
        if (index >= 0)
        {
            value = this.items[index].Value;
            return true;
        }
        else
        {
            value = default;
            return false;
        }
    }

    public List<KeyValuePair<TKey, TValue>>.Enumerator GetEnumerator() => this.items.GetEnumerator();

    IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator() => this.items.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => this.items.GetEnumerator();

    private int BinarySearch(TKey key)
    {
        return this.items.BinarySearch(new(key, default!), this.comparer);
    }

    internal bool TryFindNext(TKey key, [MaybeNullWhen(false)] out TValue? value)
    {
        int index = ~this.BinarySearch(key);
        if (index >= 0 && index < this.items.Count)
        {
            value = this.items[index].Value;
            return true;
        }

        value = default;
        return false;
    }

    private sealed class EntryKeyComparer(IComparer<TKey> keyComparer) : IComparer<KeyValuePair<TKey, TValue>>
    {
        public int Compare(KeyValuePair<TKey, TValue> x, KeyValuePair<TKey, TValue> y) =>
            keyComparer.Compare(x.Key, y.Key);

        public bool Equals(KeyValuePair<TKey, TValue> x, KeyValuePair<TKey, TValue> y) =>
            this.Compare(x, y) == 0;
    }
}
