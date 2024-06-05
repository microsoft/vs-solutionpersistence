// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.VisualStudio.SolutionPersistence.Model;

namespace Microsoft.VisualStudio.SolutionPersistence.Serializer.SlnV12;

internal sealed class SlnV12ModelExtension : ISerializerModelExtension<SlnV12SerializerSettings>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SlnV12ModelExtension"/> class.
    /// </summary>
    [SetsRequiredMembers]
    public SlnV12ModelExtension(ISolutionSerializer serializer, SlnV12SerializerSettings settings)
    {
        this.Serializer = serializer;
        this.Settings = settings;
    }

    [SetsRequiredMembers]
    public SlnV12ModelExtension(ISolutionSerializer serializer, SlnV12SerializerSettings settings, string? fullPath)
        : this(serializer, settings)
    {
        this.SolutionFileFullPath = fullPath;
    }

    public required ISolutionSerializer Serializer { get; init; }

    public string? SolutionFileFullPath { get; init; }

    public SlnV12SerializerSettings Settings { get; }
}
