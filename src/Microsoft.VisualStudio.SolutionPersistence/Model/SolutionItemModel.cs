// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.SolutionPersistence.Model;

/// <summary>
/// Represents an item in the solution model, either a project or a solution folder.
/// </summary>
public abstract partial class SolutionItemModel
{
    private List<SolutionPropertyBag>? properties;

    public SolutionFolderModel? Parent { get; private set; }

    // TODO: Make this immutable
    public Guid Id { get; set; }

    public abstract string CanonicalDisplayName { get; }

    public abstract string ItemRef { get; }

    public abstract Guid TypeId { get; }

    public IReadOnlyList<SolutionPropertyBag>? Properties => this.properties;
}
