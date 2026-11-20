// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.SolutionPersistence.Model;

/// <summary>
/// Specifies how solution items are ordered when serializing a .slnx file.
/// </summary>
public enum SolutionSortOrder
{
    /// <summary>
    /// Items are grouped by type and sorted alphabetically. This is the default.
    /// </summary>
    Alphabetical = 0,

    /// <summary>
    /// Items are written in document order, i.e. the order the elements appear in the solution file.
    /// </summary>
    Document = 1,
}
