// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.SolutionPersistence.Serializer.SlnV12;

internal static class SectionName
{
    // A property for items directory on the solution or project.
    public const string VisualStudio = "Visual Studio";
    public const string SolutionProperties = nameof(SolutionProperties);
    public const string ExtensibilityGlobals = nameof(ExtensibilityGlobals);
    public const string NestedProjects = nameof(NestedProjects);
    public const string SolutionConfigurationPlatforms = nameof(SolutionConfigurationPlatforms);
    public const string ProjectConfigurationPlatforms = nameof(ProjectConfigurationPlatforms);

    // Shared project system properties.
    public const string SharedMSBuildProjectFiles = nameof(SharedMSBuildProjectFiles);

    // Project's build dependencies.
    public const string ProjectDependencies = nameof(ProjectDependencies);

    // Solution Folder's files.
    public const string SolutionItems = nameof(SolutionItems);

    // Convert section names to the already interned constants.
    public static string InternKnownSectionName(string sectionName)
    {
        return
            StringComparer.OrdinalIgnoreCase.Equals(sectionName, SolutionProperties) ? SolutionProperties :
            StringComparer.OrdinalIgnoreCase.Equals(sectionName, ExtensibilityGlobals) ? ExtensibilityGlobals :
            StringComparer.OrdinalIgnoreCase.Equals(sectionName, NestedProjects) ? NestedProjects :
            StringComparer.OrdinalIgnoreCase.Equals(sectionName, SolutionConfigurationPlatforms) ? SolutionConfigurationPlatforms :
            StringComparer.OrdinalIgnoreCase.Equals(sectionName, ProjectConfigurationPlatforms) ? ProjectConfigurationPlatforms :
            StringComparer.OrdinalIgnoreCase.Equals(sectionName, SharedMSBuildProjectFiles) ? SharedMSBuildProjectFiles :
            StringComparer.OrdinalIgnoreCase.Equals(sectionName, ProjectDependencies) ? ProjectDependencies :
            StringComparer.OrdinalIgnoreCase.Equals(sectionName, SolutionItems) ? SolutionItems :
            sectionName;
    }
}
