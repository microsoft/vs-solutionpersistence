// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Linq;
using System.Xml;
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;
using Microsoft.VisualStudio.SolutionPersistence.Model;
using Microsoft.VisualStudio.SolutionPersistence.Utilities;

namespace Microsoft.VisualStudio.SolutionPersistence.Serializer.Xml.XmlDecorators;

/// <summary>
/// Creates an Xml DOM model for reading and updating the slnx file.
/// </summary>
[DebuggerDisplay("{Solution}")]
internal sealed class SlnxFile
{
    internal const int CurrentVersion = 1;

    internal SlnxFile(
        XmlDocument xmlDocument,
        SlnxSerializerSettings serializationSettings,
        StringTable? stringTable,
        string? fullPath)
    {
        this.Document = xmlDocument;
        this.FullPath = fullPath;
        this.StringTable = stringTable ?? new StringTable().WithSolutionConstants();

        XmlElement? xmlSolution = this.Document.DocumentElement;
        if (xmlSolution is not null && Keywords.ToKeyword(xmlSolution.Name) == Keyword.Solution)
        {
            // Expand ALL File/Project path attributes with glob patterns at raw XML parsing time
            // This happens BEFORE decorators are created, so all paths (literal and glob) go through expansion
            this.ExpandGlobPatternsInXml(xmlSolution);

            this.Solution = new XmlSolution(this, xmlSolution);
            this.Solution.UpdateFromXml();

            // This is a model part, but needs to be calculated before it can properly turn into a model.
            // These are used to calculate the actual project types from a project's Type attribute.
            this.ProjectTypes = this.Solution.GetProjectTypeTable();
        }
        else
        {
            throw new SolutionException(Errors.NotSolution, SolutionErrorType.NotSolution) { File = this.FullPath };
        }

        this.SerializationSettings = this.GetDefaultSerializationSettings(serializationSettings);
    }

    internal string? FullPath { get; }

    // Slnx file version.
    internal Version? FileVersion { get; set; }

    internal XmlDocument Document { get; }

    internal XmlSolution? Solution { get; private set; }

    internal SlnxSerializerSettings SerializationSettings { get; }

    internal StringTable StringTable { get; }

    internal ProjectTypeTable ProjectTypes { get; private set; }

    // Keep track of user project and file paths to preserve the user's path separators.
    internal Dictionary<string, string> UserPaths { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    internal bool Tarnished { get; private set; }

    internal SolutionModel ToModel()
    {
        this.UserPaths.Clear();
        SolutionModel model = this.Solution?.ToModel() ?? new SolutionModel() { StringTable = this.StringTable };
        model.SerializerExtension = new SlnXmlModelExtension(SolutionSerializers.SlnXml, this.SerializationSettings, root: this);
        return model;
    }

    /// <summary>
    /// Converts a model project path to use the slashes the user provides, or default to forward slashes.
    /// </summary>
    internal string ConvertToUserPath(string projectPath)
    {
        return this.UserPaths.TryGetValue(projectPath, out string? userProjectPath) ?
            userProjectPath :
            PathExtensions.ConvertModelToForwardSlashPath(projectPath);
    }

    /// <summary>
    /// Update the Xml DOM with changes from the model.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> if any changes were made to the XML.
    /// </returns>
    internal bool ApplyModel(SolutionModel model)
    {
        this.ProjectTypes = model.ProjectTypeTable;

        bool modified = false;
        if (this.Solution is null)
        {
            // Make the solution element the root element of the document.
            XmlElement xmlSolution = this.Document.CreateElement(Keyword.Solution.ToXmlString());
            _ = this.Document.AppendChild(xmlSolution);
            this.Solution = new XmlSolution(this, xmlSolution);
            this.Solution.UpdateFromXml();
            modified = true;
        }

        modified |= this.Solution.ApplyModelToXml(model);
        return modified;
    }

    internal string ToXmlString()
    {
        return this.Document.OuterXml;
    }

    private void ExpandGlobPatternsInElement(XmlElement element, string baseDirectory, HashSet<string> allResolvedPaths)
    {
        // Process File and Project elements in this element first
        // We need to handle them in order to support excludes correctly.
        // We iterate through a snapshot of children, but modify the live DOM.
        List<XmlNode> children = element.ChildNodes.Cast<XmlNode>().ToList();

        foreach (XmlNode childNode in children)
        {
            // Skip if the node was already removed from the DOM
            if (childNode.ParentNode is null)
            {
                continue;
            }

            if (childNode is not XmlElement childElement)
            {
                continue;
            }

            string elementName = childElement.Name;
            if (elementName != "File" && elementName != "Project")
            {
                continue;
            }

            string? pathAttribute = childElement.GetAttribute("Path");
            if (string.IsNullOrEmpty(pathAttribute))
            {
                continue;
            }

            string modelPath = PathExtensions.ConvertToModel(pathAttribute);

            if (modelPath.StartsWith('!'))
            {
                // Exclude pattern
                this.ApplyExcludePattern(element, childElement, modelPath.Substring(1), baseDirectory, allResolvedPaths);
            }
            else
            {
                // Include pattern
                this.ApplyIncludePattern(element, childElement, modelPath, baseDirectory, allResolvedPaths);
            }
        }

        // Process recursively for nested folders
        foreach (XmlNode childNode in element.ChildNodes)
        {
            if (childNode is XmlElement childElement && childElement.Name != "File" && childElement.Name != "Project")
            {
                this.ExpandGlobPatternsInElement(childElement, baseDirectory, allResolvedPaths);
            }
        }
    }

    private void ApplyExcludePattern(XmlElement parent, XmlElement excludeElement, string pattern, string baseDirectory, HashSet<string> allResolvedPaths)
    {
        // Remove the exclude element itself
        _ = parent.RemoveChild(excludeElement);

        // Find all siblings of the same type that match the pattern
        string targetElementName = excludeElement.Name;
        Matcher matcher = new(StringComparison.OrdinalIgnoreCase, preserveFilterOrder: true);
        _ = matcher.AddInclude(pattern); // We use Include here because we want to match the file path against this pattern

        // We need to iterate over current children of the parent
        List<XmlElement> siblingsToCheck = [];
        foreach (XmlNode node in parent.ChildNodes)
        {
            if (node is XmlElement el && el.Name == targetElementName)
            {
                siblingsToCheck.Add(el);
            }
        }

        foreach (XmlElement sibling in siblingsToCheck)
        {
            string? path = sibling.GetAttribute("Path");
            if (string.IsNullOrEmpty(path))
            {
                continue;
            }

            string modelPath = PathExtensions.ConvertToModel(path);

            // Check if this path matches the exclude pattern
            PatternMatchingResult result = matcher.Match(modelPath);
            if (result.HasMatches)
            {
                _ = parent.RemoveChild(sibling);
                _ = allResolvedPaths.Remove(modelPath);
            }
        }
    }

    private void ApplyIncludePattern(XmlElement parent, XmlElement includeElement, string pattern, string baseDirectory, HashSet<string> allResolvedPaths)
    {
        Matcher matcher = new(StringComparison.OrdinalIgnoreCase, preserveFilterOrder: true);
        _ = matcher.AddInclude(pattern);

        IEnumerable<string> globResults = matcher.GetResultsInFullPath(baseDirectory);
        List<string> resolvedPaths = [];

        foreach (string absolutePath in globResults)
        {
            string relativePath = Path.GetRelativePath(baseDirectory, absolutePath);
            if (allResolvedPaths.Add(relativePath))
            {
                resolvedPaths.Add(relativePath);
            }
        }

        // Handle no matches
        if (resolvedPaths.Count == 0)
        {
            // If it's a literal path (no wildcards), preserve it even if it doesn't exist (or wasn't found)
            if (allResolvedPaths.Add(pattern))
            {
                resolvedPaths.Add(pattern);
            }
        }

        // Update XML
        if (resolvedPaths.Count > 0)
        {
            // Insert new elements
            foreach (string path in resolvedPaths)
            {
                XmlElement newElement = (XmlElement)includeElement.CloneNode(deep: true);
                newElement.SetAttribute("Path", PathExtensions.ConvertModelToForwardSlashPath(path));
                _ = parent.InsertBefore(newElement, includeElement);
            }
        }

        // Remove the original element
        _ = parent.RemoveChild(includeElement);
    }

    private void ExpandGlobPatternsInXml(XmlElement solutionElement)
    {
        if (this.FullPath is null)
        {
            // Can't resolve relative paths without a base directory
            return;
        }

        string baseDirectory = Path.GetDirectoryName(this.FullPath) ?? Environment.CurrentDirectory;

        // Track all resolved paths to avoid duplicates across the entire solution
        HashSet<string> allResolvedPaths = new(StringComparer.OrdinalIgnoreCase);

        // Process all File and Project elements recursively throughout the solution
        this.ExpandGlobPatternsInElement(solutionElement, baseDirectory, allResolvedPaths);
    }

    // Fill out default values.
    private SlnxSerializerSettings GetDefaultSerializationSettings(SlnxSerializerSettings inputSettings)
    {
        string newLineChars = Environment.NewLine;
        string newIndentChars = "  ";
        if ((inputSettings.IndentChars is null || inputSettings.NewLine is null) &&
            this.Solution is not null &&
            this.Solution.TryGetFormatting(out StringSpan newLine, out StringSpan indent))
        {
            newLineChars = newLine.ToString();
            newIndentChars = indent.ToString();
        }

        return inputSettings with
        {
            PreserveWhitespace = inputSettings.PreserveWhitespace ?? this.Document.PreserveWhitespace,
            IndentChars = inputSettings.IndentChars ?? newIndentChars,
            NewLine = inputSettings.NewLine ?? newLineChars,
        };
    }
}
