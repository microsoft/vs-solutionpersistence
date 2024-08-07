// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Xml;
using Microsoft.VisualStudio.SolutionPersistence.Model;
using Microsoft.VisualStudio.SolutionPersistence.Utilities;

namespace Microsoft.VisualStudio.SolutionPersistence.Serializer.Xml.XmlDecorators;

/// <summary>
/// Child of a Solution or Folder that represents a project in the solution.
/// </summary>
internal sealed partial class XmlProject(SlnxFile root, XmlFolder? xmlParentFolder, XmlElement element) :
    XmlContainerWithProperties(root, element, Keyword.Project),
    IItemRefDecorator
{
    private ItemRefList<XmlBuildDependency> buildDependencies = new ItemRefList<XmlBuildDependency>(ignoreCase: true);
    private ItemConfigurationRulesList configurationRules = new ItemConfigurationRulesList();

    public Keyword ItemRefAttribute => Keyword.Path;

    internal string Path => this.ItemRef;

    internal StringSpan DefaultDisplayName => PathExtensions.GetStandardDisplayName(PathExtensions.ConvertFromPersistencePath(this.Path));

    internal string? DisplayName
    {
        get => this.GetXmlAttribute(Keyword.DisplayName);
        set => this.UpdateXmlAttribute(Keyword.DisplayName, value);
    }

    internal string? Type
    {
        get => this.GetXmlAttribute(Keyword.Type);
        set => this.UpdateXmlAttribute(Keyword.Type, value);
    }

    internal XmlFolder? ParentFolder { get; } = xmlParentFolder;

    /// <inheritdoc/>
    internal override XmlDecorator? ChildDecoratorFactory(XmlElement element, Keyword elementName)
    {
        return elementName switch
        {
            Keyword.BuildDependency => new XmlBuildDependency(this.Root, element),
            Keyword.BuildType => new XmlConfigurationBuildType(this.Root, element),
            Keyword.Platform => new XmlConfigurationPlatform(this.Root, element),
            Keyword.Build => new XmlConfigurationBuild(this.Root, element),
            Keyword.Deploy => new XmlConfigurationDeploy(this.Root, element),
            _ => base.ChildDecoratorFactory(element, elementName),
        };
    }

    /// <inheritdoc/>
    internal override void OnNewChildDecoratorAdded(XmlDecorator childDecorator)
    {
        switch (childDecorator)
        {
            case XmlBuildDependency buildDependency:
                this.buildDependencies.Add(buildDependency);
                break;
            case XmlConfiguration configuration:
                this.configurationRules.Add(configuration);
                break;
        }

        base.OnNewChildDecoratorAdded(childDecorator);
    }

    #region Deserialize model

    internal SolutionProjectModel AddToModel(SolutionModel solution)
    {
        SolutionFolderModel? parentFolder = null;
        if (this.ParentFolder is not null)
        {
            if (solution.FindItemByItemRef(this.ParentFolder.Name) is SolutionFolderModel foundParentFolder)
            {
                parentFolder = foundParentFolder;
            }
            else
            {
                throw SolutionException.Create(string.Format(Errors.InvalidFolderReference_Args1, this.ParentFolder.Name), this);
            }
        }

        SolutionProjectModel projectModel = solution.AddProject(
            filePath: PathExtensions.ConvertFromPersistencePath(this.Path),
            projectTypeName: this.Type ?? string.Empty,
            folder: parentFolder);

        projectModel.DisplayName = this.DisplayName;

        foreach (ConfigurationRule configurationRule in this.configurationRules.ToModel())
        {
            projectModel.AddProjectConfigurationRule(configurationRule);
        }

        foreach (XmlProperties properties in this.propertyBags.GetItems())
        {
            properties.AddToModel(projectModel);
        }

        return projectModel;
    }

    internal void AddDependenciesToModel(SolutionModel solution, SolutionProjectModel projectModel)
    {
        foreach (XmlBuildDependency buildDependency in this.buildDependencies.GetItems())
        {
            string dependencyItemRef = PathExtensions.ConvertFromPersistencePath(buildDependency.Project);
            if (solution.FindItemByItemRef(dependencyItemRef) is SolutionProjectModel dependencyProject)
            {
                projectModel.AddDependency(dependencyProject);
            }
            else
            {
                throw SolutionException.Create(string.Format(Errors.InvalidProjectReference_Args1, dependencyItemRef), buildDependency);
            }
        }
    }

    #endregion
}
