// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Linq;
using PropertyBag = Microsoft.VisualStudio.SolutionPersistence.Utilities.Lictionary<string, string>;

namespace Microsoft.VisualStudio.SolutionPersistence.Model;

public enum PropertiesScope : byte
{
    /// <summary>
    /// In Visual Studio the extensibility extensions for these properties are loaded before the solution/project is loaded.
    /// </summary>
    PreLoad,

    /// <summary>
    /// In Visual Studio the extensibility extensions for these properties are loaded after the solution/project is loaded.
    /// </summary>
    PostLoad,
}

public sealed class SolutionPropertyBag : IReadOnlyDictionary<string, string>
{
    private List<string> propertyNamesInOrder;
    private PropertyBag properties;

    public SolutionPropertyBag(string id)
        : this(id, PropertiesScope.PreLoad)
    {
    }

    public SolutionPropertyBag(string id, PropertiesScope scope)
        : this(id, scope, capacity: 0)
    {
    }

    public SolutionPropertyBag(string id, PropertiesScope scope, int capacity)
    {
        this.Id = id;
        this.Scope = scope;
        this.propertyNamesInOrder = new List<string>(capacity);
        this.properties = new PropertyBag(capacity);
    }

    // Create a new property bag that isn't frozen.
    public SolutionPropertyBag(SolutionPropertyBag propertyBag)
    {
        Argument.ThrowIfNull(propertyBag, nameof(propertyBag));
        this.Id = propertyBag.Id;
        this.Scope = propertyBag.Scope;
        this.propertyNamesInOrder = new List<string>(propertyBag.propertyNamesInOrder);
        this.properties = new PropertyBag(propertyBag.properties);
    }

    public string Id { get; }

    public PropertiesScope Scope { get; }

    public int Count => this.propertyNamesInOrder.Count;

    public IReadOnlyList<string> PropertyNames => this.propertyNamesInOrder;

#if NETFRAMEWORK
#nullable disable warnings
#endif
    public bool TryGetValue(string key, [MaybeNullWhen(false)] out string value) =>
#if NETFRAMEWORK
#nullable restore
#endif
        this.properties.TryGetValue(key, out value);

    public bool ContainsKey(string key)
    {
        return this.properties.ContainsKey(key);
    }

    public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
    {
        foreach (string key in this.propertyNamesInOrder)
        {
            yield return new KeyValuePair<string, string>(key, this.properties[key]);
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return this.GetEnumerator();
    }

    public IEnumerable<string> Keys => this.PropertyNames;

    public IEnumerable<string> Values => this.PropertyNames.Select(x => this[x]);

    public string this[string key] => this.TryGetValue(key, out string? value) ? value : throw new KeyNotFoundException();

    public void Add(string? name, string value)
    {
        if (name is null || name.Length == 0)
        {
            return;
        }

        if (this.properties.TryAdd(name, value))
        {
            this.propertyNamesInOrder.Add(name);
        }
        else
        {
            this.properties[name] = value;
        }
    }

    public void AddRange(IReadOnlyCollection<KeyValuePair<string, string>> properties)
    {
        Argument.ThrowIfNull(properties, nameof(properties));

        if (this.properties.Count == 0)
        {
            this.properties = new PropertyBag(properties);
            this.propertyNamesInOrder = properties.ToList(x => x.Key);
            return;
        }
        else
        {
            foreach ((string key, string value) in properties)
            {
                this.Add(key, value);
            }
        }
    }

    public void Remove(string name)
    {
        if (this.properties.Remove(name))
        {
            _ = this.propertyNamesInOrder.Remove(name);
        }
    }
}
