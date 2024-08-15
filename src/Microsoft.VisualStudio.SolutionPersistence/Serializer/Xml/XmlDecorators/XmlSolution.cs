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
    private ItemRefList<XmlProject> rootProjects = new ItemRefList<XmlProject>(ignoreCase: true);

    internal string? Description
    {
        get => this.GetXmlAttribute(Keyword.Description);
        set => this.UpdateXmlAttribute(Keyword.Description, value);
    }

    internal Guid SolutionId
    {
        get => this.GetXmlAttributeGuid(Keyword.SolutionId, Guid.Empty);
        set => this.UpdateXmlAttribute(Keyword.SolutionId, isDefault: value == Guid.Empty, value, static guid => guid.ToString());
    }

    internal string? VisualStudioVersion
    {
        get => this.GetXmlAttribute(Keyword.VisualStudioVersion);
        set => this.UpdateXmlAttribute(Keyword.VisualStudioVersion, value);
    }

    internal string? MinimumVisualStudioVersion
    {
        get => this.GetXmlAttribute(Keyword.MinimumVisualStudioVersion);
        set => this.UpdateXmlAttribute(Keyword.MinimumVisualStudioVersion, value);
    }

#if DEBUG

    internal override string DebugDisplay => $"{base.DebugDisplay} RootProjects={this.rootProjects} Folders={this.folders}";

#endif

    /// <inheritdoc/>
    internal override XmlDecorator? ChildDecoratorFactory(XmlElement element, Keyword elementName)
    {
        return elementName switch
        {
            Keyword.Configurations => new XmlConfigurations(this.Root, element),
            Keyword.Project => this.CreateProjectDecorator(element, xmlParentFolder: null),
            Keyword.Folder => new XmlFolder(this.Root, this, element),
            _ => base.ChildDecoratorFactory(element, elementName),
        };
    }

    internal XmlProject CreateProjectDecorator(XmlElement element, XmlFolder? xmlParentFolder)
    {
        return new XmlProject(this.Root, xmlParentFolder, element);
    }

    /// <inheritdoc/>
    internal override void OnNewChildDecoratorAdded(XmlDecorator childDecorator)
    {
        switch (childDecorator)
        {
            case XmlFolder folder:
                this.folders.Add(folder);
                break;
            case XmlProject project:
                this.rootProjects.Add(project);
                break;
            case XmlConfigurations configurations:
                this.configurationsSingle.Add(configurations);
                break;
        }

        base.OnNewChildDecoratorAdded(childDecorator);
    }

    #region Deserialize model

    internal SolutionModel ToModel()
    {
        SolutionModel solutionModel = new SolutionModel
        {
            StringTable = this.Root.StringTable,
            Description = this.Description,
            MinVsVersion = this.MinimumVisualStudioVersion,
            VsVersion = this.VisualStudioVersion,
            SolutionId = this.SolutionId.NullIfEmpty(),

            // Project types are loaded earlier when parsing the XML since they are needed to resolve projects.
            ProjectTypes = this.Root.ProjectTypes.ProjectTypes,
        };

        List<(XmlProject, SolutionProjectModel)> newProjects = new List<(XmlProject, SolutionProjectModel)>(this.rootProjects.ItemsCount);
        foreach (XmlProject project in this.rootProjects.GetItems())
        {
            newProjects.Add((project, project.AddToModel(solutionModel)));
        }

        foreach (XmlFolder folder in this.folders.GetItems())
        {
            folder.AddToModel(solutionModel, newProjects);
        }

        // Dependencies need to be added after all the projects are loaded.
        foreach ((XmlProject xmlProject, SolutionProjectModel modelProject) in newProjects)
        {
            xmlProject.AddDependenciesToModel(solutionModel, modelProject);
        }

        foreach (XmlConfigurations configurations in this.configurationsSingle.GetItems())
        {
            configurations.AddToModel(solutionModel);
        }

        // Create default configurations if they weren't provided by the Configurations section.
        // Add default build types (Debug/Release) if not specified.
        if (solutionModel.BuildTypes.IsNullOrEmpty() && solutionModel.SolutionProjects.Count > 0)
        {
            solutionModel.AddBuildType(BuildTypeNames.Debug);
            solutionModel.AddBuildType(BuildTypeNames.Release);
        }

        // Add default platform (Any CPU) if not specified.
        if (solutionModel.Platforms.IsNullOrEmpty() && solutionModel.SolutionProjects.Count > 0)
        {
            solutionModel.AddPlatform(PlatformNames.AnySpaceCPU);
        }

        foreach (XmlProperties properties in this.propertyBags.GetItems())
        {
            properties.AddToModel(solutionModel);
        }

        return solutionModel;
    }

    /// <summary>
    /// Create a project type table from the declared project types in this solution.
    /// </summary>
    internal ProjectTypeTable GetProjectTypeTable()
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

    // Try to figure out indentation and line ending default from the XML.
    internal bool TryGetFormatting(out StringSpan newLine, out StringSpan indent)
    {
        foreach (XmlDecorator decorator in this.folders.GetItems())
        {
            if (TryDecorator(decorator, newLine: out newLine, indent: out indent))
            {
                return true;
            }
        }

        foreach (XmlDecorator decorator in this.rootProjects.GetItems())
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
}
