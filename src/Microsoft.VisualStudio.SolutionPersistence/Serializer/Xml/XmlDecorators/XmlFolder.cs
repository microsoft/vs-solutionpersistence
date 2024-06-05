// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq;
using System.Xml;
using Microsoft.VisualStudio.SolutionPersistence.Model;

namespace Microsoft.VisualStudio.SolutionPersistence.Serializer.Xml.XmlDecorators;

/// <summary>
/// Child of a Solution that represents a solution folder.
/// </summary>
internal sealed class XmlFolder(SlnxFile root, XmlSolution xmlSolution, XmlElement element) :
    XmlContainerWithProperties(root, element, Keyword.Folder),
    IItemRefDecorator
{
    private readonly XmlSolution xmlSolution = xmlSolution;
    private ItemRefList<XmlFile> files = new ItemRefList<XmlFile>(ignoreCase: true);

    public Keyword ItemRefAttribute => Keyword.Name;

    public string Name => this.ItemRef;

    /// <inheritdoc/>
    public override XmlDecorator? ChildDecoratorFactory(XmlElement element, Keyword elementName)
    {
        return elementName switch
        {
            // Forward project handling to the solution decorator.
            Keyword.Project => this.xmlSolution.CreateProjectDecorator(element, xmlParentFolder: this),
            Keyword.File => new XmlFile(this.Root, element),
            _ => base.ChildDecoratorFactory(element, elementName),
        };
    }

    /// <inheritdoc/>
    public override void OnNewChildDecoratorAdded(XmlDecorator childDecorator)
    {
        switch (childDecorator)
        {
            case XmlFile file:
                this.files.Add(file);
                break;
            case XmlProject project:
                // Forward project handling to the solution decorator.
                this.xmlSolution.OnNewChildDecoratorAdded(project);
                break;
        }

        base.OnNewChildDecoratorAdded(childDecorator);
    }

    #region Deserialize model

    public void ToModelBuilder(SolutionModel.Builder solutionBuilder)
    {
        SolutionFolderModel.Builder builder = solutionBuilder.AddFolder(this.Name);

        foreach (XmlFile file in this.files.GetItems())
        {
            builder.AddFile(file.Path);
        }

        foreach (XmlProperties properties in this.propertyBags.GetItems())
        {
            _ = builder.AddProperties(properties.ToModel());
        }
    }

    #endregion

    // Update the Xml DOM with changes from the model.
    public bool ApplyModelToXml(SolutionModel modelSolution, SolutionFolderModel modelFolder)
    {
        bool modified = false;

        // Files
        modified |= this.ApplyModelToXmlGeneric(
            modelCollection: modelFolder,
            decoratorItems: ref this.files,
            decoratorElementName: Keyword.File,
            getItemRefs: static (modelFolder) => modelFolder.Files?.ToList(),
            getModelItem: static (modelFolder, itemRef) => ModelHelper.FindByItemRef(modelFolder.Files, itemRef),
            applyModelToXml: null);

        // Projects
        modified |= this.ApplyModelToXmlGeneric(
            modelCollection: modelSolution.SolutionProjects,
            ref this.xmlSolution.Projects,
            Keyword.Project,
            getItemRefs: static (modelProjects, modelFolder) => modelProjects.Where(x => x.Parent == modelFolder).Select(static x => x.ItemRef).ToList(),
            getModelItem: static (modelProjects, itemRef, modelFolder) => ModelHelper.FindByItemRef(modelProjects, itemRef),
            applyModelToXml: static (newProject, newValue, modelFolder) => newProject.ApplyModelToXml(newValue),
            modelFolder);

        // Properties
        modified |= this.ApplyModelToXml(modelFolder.Properties);

        return modified;
    }

#if DEBUG

    public override string DebugDisplay => $"{base.DebugDisplay} Files={this.files}";

#endif
}
