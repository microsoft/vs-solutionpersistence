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

    public string Path => this.ItemRef;

    public StringSpan DefaultDisplayName => this.Path.GetStandardDisplayName();

    public string? DisplayName
    {
        get => this.GetXmlAttribute(Keyword.DisplayName);
        set => this.UpdateXmlAttribute(Keyword.DisplayName, value);
    }

    public string? Type
    {
        get => this.GetXmlAttribute(Keyword.Type);
        set => this.UpdateXmlAttribute(Keyword.Type, value);
    }

    public XmlFolder? ParentFolder { get; } = xmlParentFolder;

    /// <inheritdoc/>
    public override XmlDecorator? ChildDecoratorFactory(XmlElement element, Keyword elementName)
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
    public override void OnNewChildDecoratorAdded(XmlDecorator childDecorator)
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

    public void ToModelBuilder(SolutionModel.Builder solutionBuilder)
    {
        SolutionProjectModel.Builder builder = solutionBuilder.AddProject(this.Path);
        builder.ProjectType = this.Type;
        builder.DisplayName = this.DisplayName;
        builder.Parent = this.ParentFolder?.Name;

        foreach (XmlBuildDependency buildDependency in this.buildDependencies.GetItems())
        {
            builder.AddDependency(buildDependency.Project);
        }

        foreach (ConfigurationRule configurationRule in this.configurationRules.ToModel())
        {
            builder.AddConfigurationRule(configurationRule);
        }

        foreach (XmlProperties properties in this.propertyBags.GetItems())
        {
            _ = builder.AddProperties(properties.ToModel());
        }
    }

    #endregion
}
