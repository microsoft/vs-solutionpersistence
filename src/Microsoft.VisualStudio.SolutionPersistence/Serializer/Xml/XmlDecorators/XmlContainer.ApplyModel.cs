// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Xml;
using Microsoft.VisualStudio.SolutionPersistence.Utilities;

namespace Microsoft.VisualStudio.SolutionPersistence.Serializer.Xml.XmlDecorators;

/// <summary>
/// Represents a decorator that wraps an <see cref="XmlElement"/> that is a container element.
/// These partial methods are used for updating the Xml DOM with changes from the model.
/// </summary>
internal abstract partial class XmlContainer
{
#if DEBUG

    // Diagnostic view of the child nodes.
    internal List<string> DebugChildNodes
    {
        get
        {
            List<string> nodeDescriptions = new(this.XmlElement.ChildNodes.Count);
            foreach (XmlNode xmlNode in this.XmlElement.ChildNodes)
            {
                string value = xmlNode.Value ?? string.Empty;
                string nodeDescription = xmlNode switch
                {
                    XmlWhitespace xmlWhitespace =>
                        $"Whitespace: {value.Replace('\r', '↩').Replace('\n', '↓').Replace('\t', '→').Replace(' ', '·')}",
                    XmlComment xmlComment =>
                        $"Comment:    {value.Substring(0, Math.Min(40, value.Length))}",
                    XmlElement xmlElement =>
                        $"Element:    {xmlElement.Name}",
                    XmlText xmlText =>
                        $"Text:       {value}",
                    XmlCDataSection xmlCDataSection =>
                        $"CData:      {value}",
                    _ =>
                        $"Unexpected: {xmlNode.GetType().Name} {value}",
                };
                nodeDescriptions.Add(nodeDescription);
            }

            return nodeDescriptions;
        }
    }

#endif // DEBUG

    /// <summary>
    /// Attempt to encapsulate the logic of updating the Xml DOM to match the model.
    /// Applies a model collection to the XML by updating existing elements, adding new elements, and removing elements that are no longer in the model.
    /// </summary>
    /// <typeparam name="TModelCollection">The collection represented in the model.</typeparam>
    /// <typeparam name="TModelItem">The model item in the collection.</typeparam>
    /// <typeparam name="TDecorator">The decorator representing the model item.</typeparam>
    /// <param name="modelCollection">The model collection.</param>
    /// <param name="decoratorItems">The list of existing decorator items in the XML.</param>
    /// <param name="decoratorElementName">The element name to create if new decorators are needed.</param>
    /// <param name="getItemRefs">Gets the list of item refs from the model collection.</param>
    /// <param name="getModelItem">Looks up a model item from the model collection by item ref.</param>
    /// <param name="applyModelToXml">Applies the model item changes to the decorator.</param>
    /// <returns>true if the XML was changed.</returns>
    /// <exception cref="InvalidOperationException">Thrown if <paramref name="getModelItem"/> cannot find an item that was returned from <paramref name="getItemRefs"/>.</exception>
    internal bool ApplyModelToXmlGeneric<TModelCollection, TModelItem, TDecorator>(
        TModelCollection modelCollection,
        ref ItemRefList<TDecorator> decoratorItems,
        Keyword decoratorElementName,
        Func<TModelCollection, List<string>?> getItemRefs,
        Func<TModelCollection, string, TModelItem?> getModelItem,
        Func<TDecorator, TModelItem, bool>? applyModelToXml)
        where TDecorator : XmlDecorator, IItemRefDecorator
    {
        return this.ApplyModelToXmlGeneric(
            modelCollection: modelCollection,
            decoratorItems: ref decoratorItems,
            decoratorElementName: decoratorElementName,
            getItemRefs: static (modelCollection, state) => state.getItemRefs(modelCollection),
            getModelItem: static (modelCollection, itemRef, state) => state.getModelItem(modelCollection, itemRef),
            applyModelToXml: applyModelToXml is null ? null : (static (decorator, modelItem, state) => state.applyModelToXml!(decorator, modelItem)),
            state: (getItemRefs, getModelItem, applyModelToXml));
    }

    /// <summary>
    /// Attempt to encapsulate the logic of updating the Xml DOM to match the model.
    /// Applies a model collection to the XML by updating existing elements, adding new elements, and removing elements that are no longer in the model.
    /// </summary>
    /// <typeparam name="TModelCollection">The collection represented in the model.</typeparam>
    /// <typeparam name="TModelItem">The model item in the collection.</typeparam>
    /// <typeparam name="TDecorator">The decorator representing the model item.</typeparam>
    /// <typeparam name="TState">The type of the state to pass to the functions.</typeparam>
    /// <param name="modelCollection">The model collection.</param>
    /// <param name="decoratorItems">The list of existing decorator items in the XML.</param>
    /// <param name="decoratorElementName">The element name for the decorator, can be dynamic by using getDecoratorElementName.</param>
    /// <param name="getItemRefs">Gets the list of item refs from the model collection.</param>
    /// <param name="getModelItem">Looks up a model item from the model collection by item ref.</param>
    /// <param name="applyModelToXml">Applies the model item changes to the decorator.</param>
    /// <param name="state">The state to pass to the functions.</param>
    /// <returns>true if the XML was changed.</returns>
    /// <exception cref="InvalidOperationException">Thrown if <paramref name="getModelItem"/> cannot find an item that was returned from <paramref name="getItemRefs"/>.</exception>
    internal bool ApplyModelToXmlGeneric<TModelCollection, TModelItem, TDecorator, TState>(
        TModelCollection modelCollection,
        ref ItemRefList<TDecorator> decoratorItems,
        Keyword decoratorElementName,
        Func<TModelCollection, TState, List<string>?> getItemRefs,
        Func<TModelCollection, string, TState, TModelItem?> getModelItem,
        Func<TDecorator, TModelItem, TState, bool>? applyModelToXml,
        TState state)
        where TDecorator : XmlDecorator, IItemRefDecorator
    {
        bool modified = false;

        List<string> expectedItemRefs = getItemRefs(modelCollection, state) ?? [];
        ListBuilderStruct<TDecorator> toRemove = new ListBuilderStruct<TDecorator>();

#if DEBUG

        // Make it easy to add a breakpoint on a specific element type.
        switch (decoratorElementName)
        {
            case Keyword.BuildType: break;
            case Keyword.Platform: break;
            case Keyword.Property: break;
            case Keyword.Project: break;
            case Keyword.Folder: break;
            case Keyword.Properties: break;
            case Keyword.File: break;
            case Keyword.Build: break;
            case Keyword.Deploy: break;
            case Keyword.ProjectType: break;
            default: break;
        }

#endif

        // Update existing elements and find elements that are no longer in the model.
        foreach (TDecorator decorator in decoratorItems.GetItems())
        {
            string itemRef = decorator.ItemRef;
            TModelItem? modelItem = getModelItem(modelCollection, itemRef, state);
            if (modelItem is not null)
            {
                // If this is not in expected items, it is probably from filtered decorator items collection
                // for example decoratorItems contains all projects, but this is filtered to a single folder.
                if (expectedItemRefs.Remove(itemRef) &&
                    applyModelToXml is not null &&
                    applyModelToXml(decorator, modelItem, state))
                {
                    modified = true;
                }
            }
            else
            {
                // This element is no longer in the model.
                toRemove.Add(decorator);
                modified = true;
            }
        }

        // Remove elements that are no longer in the model.
        foreach (TDecorator decoratorItem in toRemove)
        {
            this.RemoveXmlChild(decoratorItem);
            decoratorItems.Remove(decoratorItem);
        }

        // Add new elements that aren't already in the XML.
        expectedItemRefs.Sort(decoratorItems.IgnoreCase ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);
        foreach (string itemRef in expectedItemRefs)
        {
            // These values were just in the collection.
            TModelItem? modelItem = getModelItem(modelCollection, itemRef, state) ?? throw new InvalidOperationException();

            TDecorator? nextExistingITem = decoratorItems.FindNext(itemRef);

            // CONSIDER: Find position to insert before based on general areas and alphabetical order.
            TDecorator newDecorator = (TDecorator)this.CreateAndAddChild(decoratorElementName, itemRef, insertBefore: null);
            _ = applyModelToXml?.Invoke(newDecorator, modelItem, state);
            modified = true;
        }

        // Remove invalid elements.
        foreach (TDecorator invalidElements in decoratorItems.GetInvalidItems())
        {
            this.RemoveXmlChild(invalidElements);
            modified = true;
        }

        decoratorItems.ClearInvalidItems();

        return modified;
    }

    #region Manipulate XML

    private protected void RemoveXmlChild(XmlDecorator? childToRemove)
    {
        if (childToRemove is null)
        {
            return;
        }

        foreach (XmlNode node in childToRemove.GetElementAndTrivia())
        {
            // For now use the node's parent instead of this.XmlElement, because when
            // projects are moved to different folders they may not still be in this container.
            _ = node.ParentNode?.RemoveChild(node);
        }

        if (!this.XmlElement.ChildElements().Any())
        {
            // This clears out all child nodes and collapses the element to a self-closing tag.
            this.XmlElement.IsEmpty = true;
        }
    }

    /// <summary>
    /// Creates a new child element and wraps it with a new decorator.
    /// The new decorator is initialized and requested to add it to the cache.
    /// </summary>
    private protected XmlDecorator CreateAndAddChild(Keyword type, string? itemRef, XmlDecorator? insertBefore)
    {
        XmlElement newElement = this.CreateXmlChild(type, insertBefore);
        XmlDecorator? newDecorator = this.CreateChildDecorator(newElement, itemRef, validateItemRef: true);
        return newDecorator ?? throw new InvalidOperationException("Requested item doesn't not created by child factory.");
    }

    private XmlElement CreateXmlChild(Keyword type, XmlDecorator? insertBefore)
    {
        XmlElement newElement = this.XmlElement.OwnerDocument.CreateElement(type.ToXmlString());

        return insertBefore is null ?
            this.AppendChildWithWhitespace(newElement) :
            this.InsertBeforeWithWhitespace(newElement, insertBefore);
    }

    private XmlElement AppendChildWithWhitespace(XmlElement newElement)
    {
        if (this.Root.SerializationSettings.PreserveWhitespace == true)
        {
            // This is the whitespace that goes after the last child element. If it exists reuse it, otherwise this
            // indent should be the same at the parent level.
            if (this.XmlElement.LastChild is not XmlWhitespace afterWhitespace)
            {
                afterWhitespace = this.XmlElement.OwnerDocument.CreateWhitespace(this.GetNewLineAndIndent().ToString());
                _ = this.XmlElement.AppendChild(afterWhitespace);
            }

            _ = this.XmlElement.InsertBefore(newElement, afterWhitespace);

            // This is the new line whitespace between this and the previous element.
            // Just add an indent to the parent level.
            XmlWhitespace beforeWhitespace = this.XmlElement.OwnerDocument.CreateWhitespace(
                this.GetNewLineAndIndent().ToString() + this.Root.SerializationSettings.IndentChars);
            _ = this.XmlElement.InsertBefore(beforeWhitespace, newElement);
        }
        else
        {
            _ = this.XmlElement.AppendChild(newElement);
        }

        return newElement;
    }

    private XmlElement InsertBeforeWithWhitespace(XmlElement newElement, XmlDecorator insertBefore)
    {
        if (this.Root.SerializationSettings.PreserveWhitespace == true)
        {
            XmlNode insertBeforeNode = insertBefore.GetFirstTrivia();

            _ = this.XmlElement.InsertBefore(newElement, insertBeforeNode);

            // This is the new line whitespace between this and the previous element.
            // Just add an indent to the parent level.
            XmlWhitespace beforeWhitespace = this.XmlElement.OwnerDocument.CreateWhitespace(
                this.GetNewLineAndIndent().ToString() + this.Root.SerializationSettings.IndentChars);
            _ = this.XmlElement.InsertBefore(beforeWhitespace, newElement);
        }
        else
        {
            _ = this.XmlElement.AppendChild(newElement);
        }

        return newElement;
    }

    #endregion
}
