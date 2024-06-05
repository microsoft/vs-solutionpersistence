// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.SolutionPersistence.Model;

/// <summary>
/// Type of build dimension that can be configured.
/// </summary>
public enum BuildDimension : byte
{
    /// <summary>
    /// Represents the build type of a project. (For example Debug or Release).
    /// </summary>
    BuildType,

    /// <summary>
    /// Represents the platform of a project. (For example Any CPU or x64).
    /// </summary>
    Platform,

    /// <summary>
    /// Determines if the project should be built.
    /// </summary>
    Build,

    /// <summary>
    /// Determines if the project should be deployed.
    /// </summary>
    Deploy,
}

/// <summary>
/// Represents a rule that maps a solution configuration dimension to a project configuration dimension.
/// For example a Platform dimension might map "Any CPU" -> "x64" for a C++ project.
/// Dimensions: BuildType (e.g. Debug, Release), Platform (e.g. Any CPU, x64).
/// </summary>
public readonly struct ConfigurationRule(
    BuildDimension dimension,
    string solutionBuildType,
    string solutionPlatform,
    string projectValue)
{
    public readonly BuildDimension Dimension = dimension;
    public readonly string SolutionBuildType = solutionBuildType;
    public readonly string SolutionPlatform = solutionPlatform;
    public readonly string ProjectValue = projectValue;
}
