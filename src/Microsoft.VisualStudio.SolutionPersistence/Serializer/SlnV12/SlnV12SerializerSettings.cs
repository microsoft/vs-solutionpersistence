// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.SolutionPersistence.Serializer.SlnV12;

/// <summary>
/// Custom settings for the <see cref="SolutionSerializers.SlnFileV12"/> serializer.
/// </summary>
public readonly struct SlnV12SerializerSettings
{
    /// <summary>
    /// Gets encoding to use when writing the solution file.
    /// Only ASCII, UTF-8, and UTF-16 are supported.
    /// </summary>
    public Encoding? Encoding { get; init; }
}
