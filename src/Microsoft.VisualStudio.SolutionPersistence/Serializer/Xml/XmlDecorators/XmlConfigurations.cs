// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Xml;
using Microsoft.VisualStudio.SolutionPersistence.Model;

namespace Microsoft.VisualStudio.SolutionPersistence.Serializer.Xml.XmlDecorators;

/// <summary>
/// Child to a Solution that represents a collection of configurations.
/// </summary>
internal sealed class XmlConfigurations(SlnxFile root, XmlElement element) :
    XmlContainer(root, element, Keyword.Configurations),
    IItemRefDecorator
{
    private ItemRefList<XmlBuildType> buildType = new ItemRefList<XmlBuildType>(ignoreCase: true);
    private ItemRefList<XmlPlatform> platforms = new ItemRefList<XmlPlatform>(ignoreCase: true);
    private ItemRefList<XmlProjectType> projectTypes = new ItemRefList<XmlProjectType>();

    public Keyword ItemRefAttribute => Keyword.Configurations;

    /// <inheritdoc/>
    public override XmlDecorator? ChildDecoratorFactory(XmlElement element, Keyword elementName)
    {
        return elementName switch
        {
            Keyword.Platform => new XmlPlatform(this.Root, element),
            Keyword.BuildType => new XmlBuildType(this.Root, element),
            Keyword.ProjectType => new XmlProjectType(this.Root, element),
            _ => base.ChildDecoratorFactory(element, elementName),
        };
    }

    /// <inheritdoc/>
    public override void OnNewChildDecoratorAdded(XmlDecorator childDecorator)
    {
        switch (childDecorator)
        {
            case XmlPlatform platform:
                this.platforms.Add(platform);
                break;
            case XmlBuildType buildType:
                this.buildType.Add(buildType);
                break;
            case XmlProjectType projectType:
                this.projectTypes.Add(projectType);
                break;
        }

        base.OnNewChildDecoratorAdded(childDecorator);
    }

    private protected override bool AllowEmptyItemRef => true;

    private protected override string RawItemRef
    {
        get => string.Empty;
        set { }
    }

    #region Deserialize model

    public static void CreateDefaultConfigurationsIfNeeded(SolutionModel.Builder builder)
    {
        // Add default build types (Debug/Release) if not specified.
        if (builder.BuildTypes.IsNullOrEmpty() && builder.HasProjects)
        {
            builder.AddBuildType(BuildTypeNames.Debug);
            builder.AddBuildType(BuildTypeNames.Release);
        }

        // Add default platform (Any CPU) if not specified.
        if (builder.Platforms.IsNullOrEmpty() && builder.HasProjects)
        {
            builder.AddPlatform(PlatformNames.AnySpaceCPU);
        }
    }

    public void AddToModelBuilder(SolutionModel.Builder builder)
    {
        foreach (XmlPlatform platform in this.platforms.GetItems())
        {
            builder.AddPlatform(platform.Name);
        }

        foreach (XmlBuildType buildType in this.buildType.GetItems())
        {
            builder.AddBuildType(buildType.Name);
        }
    }

    /// <summary>
    /// Create a project type table from the declared project types in this solution.
    /// </summary>
    public ProjectTypeTable? GetProjectTypeTable()
    {
        List<ProjectType> declaredTypes = new List<ProjectType>(this.projectTypes.ItemsCount);
        foreach (XmlProjectType projectType in this.projectTypes.GetItems())
        {
            declaredTypes.Add(projectType.ToModel());
        }

        return declaredTypes.Count > 0 ?
            new ProjectTypeTable(declaredTypes, this.Root.Logger) :
            null;
    }

    #endregion

    // Update the Xml DOM with changes from the model.
    public bool ApplyModelToXml(SolutionModel modelSolution)
    {
        bool modified = false;

        // BuildTypes
        modified |= this.ApplyModelToXmlGeneric(
            modelCollection: modelSolution,
            decoratorItems: ref this.buildType,
            decoratorElementName: Keyword.BuildType,
            getItemRefs: static (modelSolution) => modelSolution.IsBuildTypeImplicit() ? null : new List<string>(modelSolution.BuildTypes),
            getModelItem: static (modelSolution, itemRef) => ModelHelper.FindByItemRef(modelSolution.BuildTypes, itemRef),
            applyModelToXml: null);

        // Platforms
        modified |= this.ApplyModelToXmlGeneric(
            modelCollection: modelSolution,
            decoratorItems: ref this.platforms,
            decoratorElementName: Keyword.Platform,
            getItemRefs: static (modelSolution) => modelSolution.IsPlatformImplicit() ? null : new List<string>(modelSolution.Platforms),
            getModelItem: static (modelSolution, itemRef) => ModelHelper.FindByItemRef(modelSolution.Platforms, itemRef),
            applyModelToXml: null);

        // Project Types
        modified |= this.ApplyModelToXmlGeneric(
            modelCollection: modelSolution.ProjectTypes.ToList(x => (ItemRef: XmlProjectType.GetItemRef(x.Name, x.Extension, x.ProjectTypeId), Item: x)),
            ref this.projectTypes,
            Keyword.ProjectType,
            getItemRefs: static (types) => types.ToList(static x => x.ItemRef),
            getModelItem: static (items, itemRef) => ModelHelper.FindByItemRef(items, itemRef, x => x.ItemRef).Item,
            applyModelToXml: static (newProjectTypes, newValue) => newProjectTypes.ApplyModelToXml(newValue));

        return modified;
    }
}
