// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.SolutionPersistence.Model;

// Constants for the project type table.
internal sealed partial class ProjectTypeTable
{
    private static readonly ConfigurationRule[] ClrBuildRules = [ModelHelper.CreatePlatformRule(string.Empty, PlatformNames.AnyCPU)];

    internal static readonly ConfigurationRule[] NoBuildRules = [ModelHelper.CreateNoBuildRule()];

    internal static readonly Guid VCXProj = new Guid("8BC9CEB8-8B4A-11D0-8D11-00A0C91BC942");
    internal static readonly Guid SolutionFolder = new Guid("2150E333-8FDC-42A3-9474-1A3956D46DE8");

    private static ProjectTypeTable? implicitProjectTypes;

    [SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1118:Parameter should not span multiple lines", Justification = "Creating multi-item table.")]
    private static ProjectTypeTable BuiltInTypes => implicitProjectTypes ??= new ProjectTypeTable(
        isBuiltIn: true,
        projectTypes: [

            // Base rules that apply to all project types.
            new ProjectType(
                Guid.Empty,
                rules: [

                    // Sets the project build type to be the same as the solution build type.
                    new ConfigurationRule(BuildDimension.BuildType, string.Empty, string.Empty, BuildTypeNames.All),

                    // Sets the project platform to be the same as the solution platform.
                    new ConfigurationRule(BuildDimension.Platform, string.Empty, string.Empty, PlatformNames.All),

                    // Sets the project build to true and deploy to false.
                    new ConfigurationRule(BuildDimension.Build, string.Empty, string.Empty, bool.TrueString),
                    new ConfigurationRule(BuildDimension.Deploy, string.Empty, string.Empty, bool.FalseString),
                ]),

            // Common Project System CLR projects.
            new ProjectType(new Guid("9A19103F-16F7-4668-BE54-9A1E7A4F7556"), ClrBuildRules) { Name = "Common C#", Extension = ".csproj" },
            new ProjectType(new Guid("778DAE3C-4631-46EA-AA77-85C1314464D9"), ClrBuildRules) { Name = "Common VB", Extension = ".vbproj" },
            new ProjectType(new Guid("6EC3EE1D-3C4E-46DD-8F32-0CC8E7565705"), ClrBuildRules) { Name = "Common F#", Extension = ".fsproj" },

            // Default CLR projects.
            new ProjectType(new Guid("FAE04EC0-301F-11D3-BF4B-00C04F79EFBC"), ClrBuildRules) { Name = "C#" },
            new ProjectType(new Guid("F184B08F-C81C-45F6-A57F-5ABD9991F28F"), ClrBuildRules) { Name = "VB" },
            new ProjectType(new Guid("F2A71F9B-5D33-465A-A702-920D77279786"), ClrBuildRules) { Name = "F#" },

            // CLR shared code project
            new ProjectType(new Guid("D954291E-2A0B-460D-934E-DC6B0785DB48"), NoBuildRules) { Name = "Shared", Extension = ".shproj" },

            // Website project
            new ProjectType(new Guid("E24C65DC-7377-472B-9ABA-BC803B73C61A"), ClrBuildRules) { Name = "Website" },

            // Visual C++ project
            new ProjectType(
                VCXProj,
                rules: [
                    ModelHelper.CreatePlatformRule(PlatformNames.AnyCPU, PlatformNames.x64),
                    ModelHelper.CreatePlatformRule(PlatformNames.x86, PlatformNames.Win32),
                ])
            { Name = "VC", Extension = ".vcxproj" },

            // Visual C++ shared code project.
            // This is a special project type that is used to represent shared items in C++ projects.
            // It uses the same project type id as vcxproj, but doesn't have configurations.
            // It does not specify a name since it shares a project type id with vcxproj,
            // that way 'VC' is always used as the friendly name for the VCXProj guid.
            new ProjectType(VCXProj, NoBuildRules) { Extension = ".vcxitems" },

            // Exe project type
            new ProjectType(new Guid("911E67C6-3D85-4FCE-B560-20A9C3E3FF48"), NoBuildRules) { Name = "Exe", Extension = ".exe" },

            // This probably won't get used, but adding to make sure it doesn't see configurations.
            new ProjectType(SolutionFolder, NoBuildRules) { Name = "Folder" },
        ],
        logger: null);
}
