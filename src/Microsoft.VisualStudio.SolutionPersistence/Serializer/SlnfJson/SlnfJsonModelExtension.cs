// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.VisualStudio.SolutionPersistence.Model;

namespace Microsoft.VisualStudio.SolutionPersistence.Serializer.SlnfJson;

internal sealed class SlnfJsonModelExtension(ISolutionSerializer serializer, SlnfJsonSerializerSettings settings)
    : ISerializerModelExtension<SlnfJsonSerializerSettings>
{
    [SetsRequiredMembers]
    public SlnfJsonModelExtension(ISolutionSerializer serializer, SlnfJsonSerializerSettings settings, string? fullPath)
        : this(serializer, settings)
    {
        this.SolutionFileFullPath = fullPath;
    }

    /// <inheritdoc/>
    public ISolutionSerializer Serializer { get; init; } = serializer;

    /// <inheritdoc/>
    public bool Tarnished { get; init; }

    /// <inheritdoc/>
    public SlnfJsonSerializerSettings Settings { get; } = settings;

    internal string? SolutionFileFullPath { get; init; }

}
