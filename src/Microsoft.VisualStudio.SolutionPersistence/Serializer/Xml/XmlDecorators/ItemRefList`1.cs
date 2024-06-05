// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Microsoft.VisualStudio.SolutionPersistence.Utilities;

namespace Microsoft.VisualStudio.SolutionPersistence.Serializer.Xml.XmlDecorators;

/// <summary>
/// This provides a list of items that are referenced by a unique identifier.
/// This is used to cache unique items from the Xml DOM so they can be quickly referenced.
/// The list is a type of dictionary where the ItemRef is the key. If a duplicate key
/// or invalid key is found it is added to to an invalid list so it can be removed during
/// the next save.
/// </summary>
/// <typeparam name="T">The decorator type this represents.</typeparam>
/// <param name="ignoreCase">Should this consider keys with different cases the same.</param>
[DebuggerDisplay("{items?.Count} Items, {invalidItems?.Count} Invalid Items")]
internal struct ItemRefList<T>(bool ignoreCase)
    where T : XmlDecorator, IItemRefDecorator
{
    private readonly Lictionary<string, T> items = new Lictionary<string, T>(0, ignoreCase ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);
    private List<T>? invalidItems;

    public ItemRefList()
        : this(ignoreCase: false)
    {
    }

    public readonly bool IgnoreCase { get; } = ignoreCase;

    public void Add(T item)
    {
        // Missing Name attribute.
        if (!item.IsValid() || item.ItemRef is null)
        {
            item.Root.Logger.LogWarning($"Missing or invalid {item.ItemRefAttribute} attribute in {item.ElementName} element.", item.XmlElement);
            this.invalidItems ??= [];
            this.invalidItems.Add(item);
            return;
        }
        else
        {
            if (!this.items.TryAdd(item.ItemRef, item))
            {
                // Duplicate Name attribute.
                item.Root.Logger.LogWarning($"Duplicate {item.ItemRefAttribute} attribute ({item.ItemRef}) in {item.ElementName} element.", item.XmlElement);
                this.invalidItems ??= [];
                this.invalidItems.Add(item);
                return;
            }
        }
    }

    public readonly void Remove(T item)
    {
        _ = this.items.Remove(item.ItemRef);
    }

    /// <summary>
    /// Finds the item that would be next in the list after the given item.
    /// </summary>
    public readonly T? FindNext(string itemRef)
    {
        return this.items.TryFindNext(itemRef, out T? next) ? next : null;
    }

    public readonly int ItemsCount => this.items.Count;

    public readonly EnumForwarder GetItems()
    {
        return new EnumForwarder(this);
    }

    public ref struct EnumForwarder(ItemRefList<T> me)
    {
        public readonly ItemsEnumerator GetEnumerator() => new ItemsEnumerator(me.items.GetEnumerator());
    }

    public ref struct ItemsEnumerator(List<KeyValuePair<string, T>>.Enumerator enumerator)
    {
        public bool MoveNext() => enumerator.MoveNext();

        public T Current => enumerator.Current.Value;
    }

    public readonly int InvalidItemsCount => this.invalidItems?.Count ?? 0;

    public readonly ReadOnlyListStructEnumerable<T> GetInvalidItems()
    {
        return this.invalidItems.GetStructEnumerable();
    }

    public void ClearInvalidItems()
    {
        this.invalidItems = null;
    }

    internal sealed class OrdinalComparer : IComparer<T>
    {
        public static readonly OrdinalComparer Instance = new OrdinalComparer();

        public int Compare(T? x, T? y) => StringComparer.Ordinal.Compare(x?.ItemRef, y?.ItemRef);
    }

    internal sealed class OrdinalIgnoreCaseComparer : IComparer<T>
    {
        public static readonly OrdinalIgnoreCaseComparer Instance = new OrdinalIgnoreCaseComparer();

        public int Compare(T? x, T? y) => StringComparer.OrdinalIgnoreCase.Compare(x?.ItemRef, y?.ItemRef);
    }
}
