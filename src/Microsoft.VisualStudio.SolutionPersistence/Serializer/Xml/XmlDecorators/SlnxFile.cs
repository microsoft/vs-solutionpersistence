// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Xml;
using Microsoft.VisualStudio.SolutionPersistence.Model;
using Microsoft.VisualStudio.SolutionPersistence.Utilities;

namespace Microsoft.VisualStudio.SolutionPersistence.Serializer.Xml.XmlDecorators;

/// <summary>
/// Creates an Xml DOM model for reading and updating the slnx file.
/// </summary>
[DebuggerDisplay("{Solution}")]
internal sealed class SlnxFile
{
    public string? FullPath { get; }

    public SlnxFile(
        ISolutionSerializer serializer,
        XmlDocument xmlDocument,
        SlnxSerializerSettings serializationSettings,
        StringTable? stringTable,
        string? fullPath)
    {
        this.Serializer = serializer;
        this.Document = xmlDocument;
        this.FullPath = fullPath;
        this.StringTable = stringTable ?? new StringTable().WithSolutionConstants();

        XmlElement? xmlSolution = this.Document.DocumentElement;
        if (xmlSolution is not null && Keywords.ToKeyword(xmlSolution.Name) == Keyword.Solution)
        {
            this.Solution = new XmlSolution(this, xmlSolution);
            this.Solution.UpdateFromXml();

            // This is a model part, but needs to be calculated before it can properly turn into a model.
            // These are used to calculate the actual project types from a project's Type attribute.
            this.ProjectTypes = this.Solution.GetProjectTypeTable();
        }
        else
        {
            this.Logger.LogError("The root element of the slnx file is not a Solution element.");
            this.ProjectTypes = new ProjectTypeTable();
        }

        this.SerializationSettings = this.GetDefaultSerializationSettings(serializationSettings);
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

    public XmlDocument Document { get; }

    public ISolutionSerializer Serializer { get; }

    public XmlSolution? Solution { get; private set; }

    public SlnxSerializerSettings SerializationSettings { get; }

    public StringTable StringTable { get; }

    public SerializerLogger Logger { get; private set; } = new SerializerLogger();

    public ProjectTypeTable ProjectTypes { get; private set; }

    public SolutionModel ToModel()
    {
        SolutionModel.Builder? builder = this.Solution?.ToModelBuilder();
        builder ??= new SolutionModel.Builder(stringTable: this.StringTable) { Corrupted = true };

        return builder.ToModel(new SlnXmlModelExtension(this.Serializer, this.SerializationSettings, root: this));
    }

    /// <summary>
    /// Update the Xml DOM with changes from the model.
    /// </summary>
    public bool ApplyModel(SolutionModel model)
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
}
