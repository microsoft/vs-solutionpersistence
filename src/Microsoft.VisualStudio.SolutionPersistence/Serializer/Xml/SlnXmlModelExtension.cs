// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.VisualStudio.SolutionPersistence.Model;
using Microsoft.VisualStudio.SolutionPersistence.Serializer.Xml.XmlDecorators;

namespace Microsoft.VisualStudio.SolutionPersistence.Serializer.Xml;

internal sealed record SlnXmlModelExtension : ISerializerModelExtension<SlnxSerializerSettings>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SlnXmlModelExtension"/> class.
    /// </summary>
    [SetsRequiredMembers]
    public SlnXmlModelExtension(ISolutionSerializer serializer, SlnxSerializerSettings settings)
    {
        this.Serializer = serializer;
        this.Settings = settings;
    }

    [SetsRequiredMembers]
    public SlnXmlModelExtension(ISolutionSerializer serializer, SlnxSerializerSettings settings, SlnxFile root)
        : this(serializer, settings)
    {
        this.Root = root;
    }

    public required ISolutionSerializer Serializer { get; init; }

    public SlnxFile? Root { get; init; }

    public required SlnxSerializerSettings Settings { get; init; }

    public string? SolutionFileFullPath => this.Root?.FullPath;
}
