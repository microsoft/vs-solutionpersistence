// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.SolutionPersistence.Model;

/// <summary>
/// Represents a solution folder in the solution model.
/// </summary>
public sealed partial class SolutionFolderModel : SolutionItemModel
{
    private const string CycleBreaker = "***"; // to ensure no cycles
    private string? refId; // folder fullPath
    private List<string>? files;

    private SolutionFolderModel(string name) => this.Name = name;

    public IReadOnlyList<string>? Files => this.files;

    public string Name { get; }

    public override string CanonicalDisplayName => this.Name ?? string.Empty;

    public override Guid TypeId => ProjectTypeTable.SolutionFolder;

    public override string ItemRef
    {
        get
        {
            if (this.refId is not null)
            {
                return this.refId;
            }

            if (this.Parent is not null)
            {
                this.refId = CycleBreaker;
                string parentRef = this.Parent.ItemRef;
                if (!object.ReferenceEquals(parentRef, CycleBreaker))
                {
                    this.refId = $"{parentRef}{this.Name}/";
                    return this.refId;
                }
            }

            // no parent, or part of cycle move it on top.
            // potential duplicates in this case will be ignored/merged on save.
            this.refId = $"/{this.Name}/";
            return this.refId;
        }
    }
}
