// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Xml;
using Microsoft.VisualStudio.SolutionPersistence.Model;
using Microsoft.VisualStudio.SolutionPersistence.Utilities;

namespace Microsoft.VisualStudio.SolutionPersistence.Serializer.Xml.XmlDecorators;

/// <summary>
/// Child of a Project that represents a configuration mapping from a solution configuration to a project configuration.
/// </summary>
internal abstract class XmlConfiguration(SlnxFile root, XmlElement element, Keyword elementName) :
    XmlDecorator(root, element, elementName),
    IItemRefDecorator
{
    public Keyword ItemRefAttribute => Keyword.Solution;

    private protected override bool AllowEmptyItemRef => true;

    public abstract BuildDimension Dimension { get; }

    public string Solution
    {
        get => this.GetXmlAttribute(Keyword.Solution) ?? string.Empty;
        set => this.UpdateXmlAttribute(Keyword.Solution, value);
    }

    public string Project
    {
        get => this.GetXmlAttribute(Keyword.Project) ?? string.Empty;
        set => this.UpdateXmlAttribute(Keyword.Project, value);
    }

    #region Deserialize model

    public ConfigurationRule? ToModel()
    {
        BuildDimension dimension = this.Dimension;

        // Set default value for build rule to 'true' and deploy rule to 'false'.
        string projectValue =
            this.Project.NullIfEmpty() ??
            dimension switch
            {
                BuildDimension.Build or BuildDimension.Deploy => bool.TrueString,
                _ => string.Empty,
            };

        if (string.IsNullOrEmpty(projectValue))
        {
            this.Root.Logger.LogWarning("Project attribute is empty.", this.XmlElement);
            return null;
        }

        if (!ModelHelper.TrySplitFullConfiguration(this.Solution, out StringSpan solutionBuildType, out StringSpan solutionPlatform) &&
            !this.Solution.IsNullOrEmpty())
        {
            this.Root.Logger.LogWarning("Solution configuration could not be parsed.", this.XmlElement);
            return null;
        }

        if (solutionBuildType is BuildTypeNames.All)
        {
            solutionBuildType = StringSpan.Empty;
        }

        if (solutionPlatform is PlatformNames.All)
        {
            solutionPlatform = StringSpan.Empty;
        }

        // A configuration element represents a "Configuration" mapping rule.
        return new ConfigurationRule(
            dimension,
            solutionBuildType: this.GetTableString(BuildTypeNames.ToStringKnown(solutionBuildType)),
            solutionPlatform: this.GetTableString(PlatformNames.ToStringKnown(solutionPlatform)),
            projectValue: this.GetTableString(projectValue));
    }

    #endregion

    // Update the Xml DOM with changes from the model.
    public bool ApplyModelToXml(ConfigurationRule configurationRule)
    {
        // Set default value for build rule to 'true' and deploy rule to 'false'.
        string value = configurationRule.Dimension switch
        {
            BuildDimension.Build or BuildDimension.Deploy when bool.Parse(configurationRule.ProjectValue) => string.Empty,
            _ => configurationRule.ProjectValue,
        };

        if (StringComparer.Ordinal.Equals(this.Project, value))
        {
            return false;
        }

        this.Project = value;
        return true;
    }
}
