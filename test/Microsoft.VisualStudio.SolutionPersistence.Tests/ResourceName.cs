// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/// <summary>
/// Represents a resource name and an identifier.
/// </summary>
/// <param name="Name">The resource name.</param>
/// <param name="FullResourceId">The fully qualified resource identifier.</param>
public record struct ResourceName(string Name, string FullResourceId);
