// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Xml;
using Microsoft.VisualStudio.SolutionPersistence.Model;

namespace Microsoft.VisualStudio.SolutionPersistence.Serializer.Xml.XmlDecorators;

/// <summary>
/// Represents the root Solution XML element in the slnx file.
/// </summary>
internal sealed partial class XmlSolution(SlnxFile file, XmlElement element) :
    XmlContainerWithProperties(file, element, Keyword.Solution)
{
    private ItemRefList<XmlConfigurations> configurationsSingle = new ItemRefList<XmlConfigurations>();
    private ItemRefList<XmlFolder> folders = new ItemRefList<XmlFolder>(ignoreCase: true);
#pragma warning disable SA1401 // Fields should be private
    internal ItemRefList<XmlProject> Projects = new ItemRefList<XmlProject>(ignoreCase: true);
#pragma warning restore SA1401 // Fields should be private

    public string? Description
    {
        get => this.GetXmlAttribute(Keyword.Description);
        set => this.UpdateXmlAttribute(Keyword.Description, value);
    }

    public Guid SolutionId
    {
        get => this.GetXmlAttributeGuid(Keyword.SolutionId, this.DefaultSolutionId);
        set => this.UpdateXmlAttribute(Keyword.SolutionId, isDefault: value == this.DefaultSolutionId, value, static guid => guid.ToString());
    }

    public bool IsDefaultId => this.SolutionId == this.DefaultSolutionId;

    // TODO: Use calculated value as default.
    public Guid DefaultSolutionId { get; } = Guid.Empty;

    public string? VisualStudioVersion
    {
        get => this.GetXmlAttribute(Keyword.VisualStudioVersion);
        set => this.UpdateXmlAttribute(Keyword.VisualStudioVersion, value);
    }

    public string? MinimumVisualStudioVersion
    {
        get => this.GetXmlAttribute(Keyword.MinimumVisualStudioVersion);
        set => this.UpdateXmlAttribute(Keyword.MinimumVisualStudioVersion, value);
    }

    /// <inheritdoc/>
    public override XmlDecorator? ChildDecoratorFactory(XmlElement element, Keyword elementName)
    {
        return elementName switch
        {
            Keyword.Configurations => new XmlConfigurations(this.Root, element),
            Keyword.Project => this.CreateProjectDecorator(element, xmlParentFolder: null),
            Keyword.Folder => new XmlFolder(this.Root, this, element),
            _ => base.ChildDecoratorFactory(element, elementName),
        };
    }

    public XmlProject CreateProjectDecorator(XmlElement element, XmlFolder? xmlParentFolder)
    {
        return new XmlProject(this.Root, xmlParentFolder, element);
    }

    /// <inheritdoc/>
    public override void OnNewChildDecoratorAdded(XmlDecorator childDecorator)
    {
        switch (childDecorator)
        {
            case XmlFolder folder:
                this.folders.Add(folder);
                break;
            case XmlProject project:
                this.Projects.Add(project);
                break;
            case XmlConfigurations configurations:
                this.configurationsSingle.Add(configurations);
                break;
        }

        base.OnNewChildDecoratorAdded(childDecorator);
    }

    #region Deserialize model

    public SolutionModel.Builder ToModelBuilder()
    {
        SolutionModel.Builder builder = new SolutionModel.Builder(stringTable: this.Root.StringTable)
        {
            Description = this.Description,
            MinVsVersion = this.MinimumVisualStudioVersion,
            VsVersion = this.VisualStudioVersion,
            SolutionId = this.IsDefaultId ? null : this.SolutionId,

            // This table was calculated during the parsing of the XML, since it is needed to resolve project types.
            ProjectTypeTable = this.Root.ProjectTypes,
        };

        foreach (XmlFolder folder in this.folders.GetItems())
        {
            folder.ToModelBuilder(builder);
        }

        foreach (XmlProject project in this.Projects.GetItems())
        {
            project.ToModelBuilder(builder);
        }

        foreach (XmlConfigurations configurations in this.configurationsSingle.GetItems())
        {
            configurations.AddToModelBuilder(builder);
        }

        // Create default configurations if they weren't provided by the Configurations section.
        XmlConfigurations.CreateDefaultConfigurationsIfNeeded(builder);

        foreach (XmlProperties properties in this.propertyBags.GetItems())
        {
            _ = builder.AddProperties(properties.ToModel());
        }

        return builder;
    }

    /// <summary>
    /// Create a project type table from the declared project types in this solution.
    /// </summary>
    public ProjectTypeTable GetProjectTypeTable()
    {
        foreach (XmlConfigurations xmlConfigurations in this.configurationsSingle.GetItems())
        {
            ProjectTypeTable? propertyTypeTable = xmlConfigurations.GetProjectTypeTable();
            if (propertyTypeTable is not null)
            {
                return propertyTypeTable;
            }
        }

        return new ProjectTypeTable();
    }

    #endregion

    public bool TryGetFormatting(out StringSpan newLine, out StringSpan indent)
    {
        foreach (XmlDecorator decorator in this.folders.GetItems())
        {
            if (TryDecorator(decorator, newLine: out newLine, indent: out indent))
            {
                return true;
            }
        }

        foreach (XmlDecorator decorator in this.Projects.GetItems())
        {
            if (TryDecorator(decorator, newLine: out newLine, indent: out indent))
            {
                return true;
            }
        }

        foreach (XmlDecorator decorator in this.propertyBags.GetItems())
        {
            if (TryDecorator(decorator, newLine: out newLine, indent: out indent))
            {
                return true;
            }
        }

        foreach (XmlConfigurations configurations in this.configurationsSingle.GetItems())
        {
            if (TryDecorator(configurations, newLine: out newLine, indent: out indent))
            {
                return true;
            }
        }

        newLine = StringSpan.Empty;
        indent = StringSpan.Empty;
        return false;

        static bool TryDecorator(XmlDecorator decorator, out StringSpan newLine, out StringSpan indent)
        {
            StringSpan both = decorator.GetNewLineAndIndent();
            if (both.IsEmpty)
            {
                newLine = StringSpan.Empty;
                indent = StringSpan.Empty;
                return false;
            }

            indent = both.TrimStart(['\n', '\r']);
            newLine = both.Slice(0, both.Length - indent.Length);
            return true;
        }
    }

#if DEBUG

    public override string DebugDisplay => $"{base.DebugDisplay} Projects={this.Projects} Folders={this.folders}";

#endif

}
