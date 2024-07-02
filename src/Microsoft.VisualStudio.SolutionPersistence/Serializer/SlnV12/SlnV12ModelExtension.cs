// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.VisualStudio.SolutionPersistence.Model;

namespace Microsoft.VisualStudio.SolutionPersistence.Serializer.SlnV12;

/// <summary>
/// Initializes a new instance of the <see cref="SlnV12ModelExtension"/> class.
/// </summary>
[method: SetsRequiredMembers]
internal sealed class SlnV12ModelExtension(ISolutionSerializer serializer, SlnV12SerializerSettings settings)
    : ISerializerModelExtension<SlnV12SerializerSettings>
{
    [SetsRequiredMembers]
    public SlnV12ModelExtension(ISolutionSerializer serializer, SlnV12SerializerSettings settings, string? fullPath)
        : this(serializer, settings)
    {
        this.SolutionFileFullPath = fullPath;
    }

    public required ISolutionSerializer Serializer { get; init; } = serializer;

    public SlnV12SerializerSettings Settings { get; } = settings;

    internal string? SolutionFileFullPath { get; init; }

    internal bool Corrupted { get; init; }
}
