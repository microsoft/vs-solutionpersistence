// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Microsoft.VisualStudio.SolutionPersistence.Model;
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

    internal readonly bool IgnoreCase { get; } = ignoreCase;

    internal readonly int ItemsCount => this.items.Count;

    internal readonly int InvalidItemsCount => this.invalidItems?.Count ?? 0;

    internal readonly void Add(T item)
    {
        // Missing Name attribute.
        if (!item.IsValid() || item.ItemRef is null)
        {
            throw SolutionException.Create(string.Format(Errors.InvalidItemRef_Args2, item.ItemRefAttribute, item.ElementName), item);
        }
        else
        {
            if (!this.items.TryAdd(item.ItemRef, item))
            {
                // Duplicate Name attribute.
                throw SolutionException.Create(string.Format(Errors.DuplicateItemRef_Args2, item.ItemRef, item.ElementName), item);
            }
        }
    }

    internal readonly void Remove(T item)
    {
        _ = this.items.Remove(item.ItemRef);
    }

    /// <summary>
    /// Finds the item that would be next in the list after the given item.
    /// </summary>
    internal readonly T? FindNext(string itemRef)
    {
        return this.items.TryFindNext(itemRef, out T? next) ? next : null;
    }

    internal readonly EnumForwarder GetItems()
    {
        return new EnumForwarder(this);
    }

    internal readonly ReadOnlyListStructEnumerable<T> GetInvalidItems()
    {
        return this.invalidItems.GetStructEnumerable();
    }

    internal void ClearInvalidItems()
    {
        this.invalidItems = null;
    }

    internal ref struct EnumForwarder(ItemRefList<T> me)
    {
        public readonly ItemsEnumerator GetEnumerator() => new ItemsEnumerator(me.items.GetEnumerator());
    }

    internal ref struct ItemsEnumerator(List<KeyValuePair<string, T>>.Enumerator enumerator)
    {
        public T Current => enumerator.Current.Value;

        public bool MoveNext() => enumerator.MoveNext();
    }

    private sealed class OrdinalComparer : IComparer<T>
    {
        internal static readonly OrdinalComparer Instance = new OrdinalComparer();

        public int Compare(T? x, T? y) => StringComparer.Ordinal.Compare(x?.ItemRef, y?.ItemRef);
    }

    private sealed class OrdinalIgnoreCaseComparer : IComparer<T>
    {
        internal static readonly OrdinalIgnoreCaseComparer Instance = new OrdinalIgnoreCaseComparer();

        public int Compare(T? x, T? y) => StringComparer.OrdinalIgnoreCase.Compare(x?.ItemRef, y?.ItemRef);
    }
}
