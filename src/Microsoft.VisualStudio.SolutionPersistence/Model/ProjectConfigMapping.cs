﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.SolutionPersistence.Model;

/// <summary>
/// Represents the project configuration and build/deploy settings.
/// This is used to create an expanded mapping of every solution configuration to every project configuration.
/// </summary>
[method: SetsRequiredMembers]
internal readonly struct ProjectConfigMapping(string buildType, string platform, bool build, bool deploy)
{
    public required string BuildType { get; init; } = buildType;

    public required string Platform { get; init; } = platform;

    public bool Build { get; init; } = build;

    public bool Deploy { get; init; } = deploy;

    internal readonly bool IsValidBuildType => !string.IsNullOrEmpty(this.BuildType) && this.BuildType != BuildTypeNames.All;

    internal readonly bool IsValidPlatform => !string.IsNullOrEmpty(this.Platform) && this.Platform != PlatformNames.All;

    public readonly bool IsSame(in ProjectConfigMapping other)
    {
        return other.Build == this.Build &&
            other.Deploy == this.Deploy &&
            StringComparer.Ordinal.Equals(this.BuildType, other.BuildType) &&
            (this.Platform == other.Platform || StringComparer.Ordinal.Equals(this.Platform.Canonical(), other.Platform.Canonical()));
    }
}
