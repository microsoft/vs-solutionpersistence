// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Xml;
using Microsoft.VisualStudio.SolutionPersistence.Model;

namespace Microsoft.VisualStudio.SolutionPersistence.Serializer.Xml.XmlDecorators;

/// <summary>
/// Represents a decorator that wraps an <see cref="XmlElement"/> that is a container element with properties.
/// </summary>
internal abstract partial class XmlContainerWithProperties(SlnxFile root, XmlElement element, Keyword elementName) :
    XmlContainer(root, element, elementName)
{
#pragma warning disable SA1401 // Fields should be private
    private protected ItemRefList<XmlProperties> propertyBags = new ItemRefList<XmlProperties>();
#pragma warning restore SA1401 // Fields should be private

    /// <inheritdoc/>
    public override XmlDecorator? ChildDecoratorFactory(XmlElement element, Keyword elementName)
    {
        return elementName switch
        {
            Keyword.Properties => new XmlProperties(this.Root, element),
            _ => base.ChildDecoratorFactory(element, elementName),
        };
    }

    /// <inheritdoc/>
    public override void OnNewChildDecoratorAdded(XmlDecorator childDecorator)
    {
        switch (childDecorator)
        {
            case XmlProperties properties:
                this.propertyBags.Add(properties);
                break;
        }

        base.OnNewChildDecoratorAdded(childDecorator);
    }

    // Update the Xml DOM with changes from the model.
    public bool ApplyModelToXml(IReadOnlyList<SolutionPropertyBag>? modelPropertyBags)
    {
        return this.ApplyModelToXmlGeneric(
            modelCollection: modelPropertyBags ?? [],
            decoratorItems: ref this.propertyBags,
            decoratorElementName: Keyword.Properties,
            getItemRefs: static (modelPropertyBags) => modelPropertyBags.ToList(x => x.Id),
            getModelItem: static (modelPropertyBags, itemRef) => ModelHelper.FindByItemRef(modelPropertyBags, itemRef),
            applyModelToXml: static (newProperties, newValue) => newProperties.ApplyModelToXml(newValue));
    }
}
