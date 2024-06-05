// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Xml;

namespace Microsoft.VisualStudio.SolutionPersistence.Serializer.Xml;

internal static class XmlDomUtilities
{
    public static XmlElementAttributes Attributes(this XmlElement? element) => new XmlElementAttributes(element?.Attributes);

    public static XmlElementSubElementsEnumerable ChildElements(this XmlNode? element) => new XmlElementSubElementsEnumerable(element, filterByName: null);
}

/// <summary>
/// Provides a way to enumerate over xml attributes.
/// </summary>
internal ref struct XmlElementAttributes(XmlAttributeCollection? element)
{
    private int index = -1;

    public readonly XmlElementAttributes GetEnumerator() => new XmlElementAttributes(element);

    public readonly int Count => element?.Count ?? 0;

    public readonly XmlAttribute Current => element![this.index];

    public bool MoveNext()
    {
        if (element is null || this.index >= element.Count)
        {
            return false;
        }

        return ++this.index < element.Count;
    }
}

internal readonly ref struct XmlElementSubElementsEnumerable(XmlNode? element, string? filterByName)
{
    public readonly XmlElementSubElements GetEnumerator() => new XmlElementSubElements(element, filterByName);

    internal readonly bool Any()
    {
        foreach (XmlElement any in this)
        {
            return true;
        }

        return false;
    }
}

/// <summary>
/// Provides a way to enumerate over xml child elements.
/// </summary>
internal ref struct XmlElementSubElements(XmlNode? element, string? filterByName)
{
    private XmlNode? child;

    public readonly XmlElement Current => (object.ReferenceEquals(this.child, element) ? null : this.child as XmlElement)!;

    public bool MoveNext()
    {
        // use element as "sentinel end value", null as before first. (if element is null it is also an end as coincidence).
        if (object.ReferenceEquals(this.child, element) || element is null)
        {
            return false;
        }

        do
        {
            this.child = this.child is null ? element.FirstChild : this.child.NextSibling;
            if (this.child is XmlElement)
            {
                if (filterByName is null || this.child.Name == filterByName)
                {
                    return true;
                }
            }
        }
        while (this.child is not null);

        this.child = element;
        return false;
    }
}
