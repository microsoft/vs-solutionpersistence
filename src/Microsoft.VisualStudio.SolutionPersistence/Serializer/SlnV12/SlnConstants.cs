// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.SolutionPersistence.Serializer.SlnV12;

internal static class SlnConstants
{
    public const string ProjectSeparators = " ()=\",";
    public const string SectionSeparators = " \t()=";
    public const string SectionSeparators2 = "\t()=";
    public const string VersionSeparators = " =";
    public const char DoubleQuote = '"';

    public const string SLNFileHeaderNoVersion = "Microsoft Visual Studio Solution File, Format Version";
    public const string SLNFileHeaderVersion = " 12.00";

    // Special property Visual Studio property names
    public const string OpenWith = nameof(OpenWith);
    public const string HideSolutionNode = nameof(HideSolutionNode);
    public const string SolutionGuid = nameof(SolutionGuid);

    // Special property names
    public const string Description = nameof(Description);

    // Used in .SLN to determine with version of VS to open when opening from explorer.
    public const string OpenWithPrefix = "# Visual Studio Version ";

    public const string TagProjectStart = "Project(";
    public const string TagProjectSectionStart = "ProjectSection(";
    public const string TagGlobalSectionStart = "GlobalSection(";

    public const string TagProject = "Project";
    public const string TagGlobal = "Global";
    public const string TagSection = "Section";
    public const string TagGlobalSection = "GlobalSection";
    public const string TagProjectSection = "ProjectSection";

    public const string TagEndProject = "EndProject";
    public const string TagEndGlobal = "EndGlobal";
    public const string TagEndGlobalSection = "EndGlobalSection";
    public const string TagEndProjectSection = "EndProjectSection";

    public const string TagPreSolution = "preSolution";
    public const string TagPostSolution = "postSolution";
    public const string TagPreProject = "preProject";
    public const string TagPostProject = "postProject";

    public const string TagVisualStudioVersion = "VisualStudioVersion";
    public const string TagMinimumVisualStudioVersion = "MinimumVisualStudioVersion";
    public const string TagAssignValue = " = ";
    public const string TagQuoteCommaQuote = "\", \"";
    public const string TagTabTab = "\t\t";

    // This should only be use in SLN files.
    public static string ToSlnString(this Guid guid) => guid.ToString("B").ToUpperInvariant();
}
