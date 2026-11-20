// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/// <summary>
/// Represents a resource loaded from the assembly.
/// </summary>
/// <param name="Name">The resource name.</param>
/// <param name="Stream">The resource data.</param>
public record struct ResourceStream(string Name, Stream Stream);
