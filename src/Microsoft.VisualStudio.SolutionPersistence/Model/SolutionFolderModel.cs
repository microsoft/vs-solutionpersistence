// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.VisualStudio.SolutionPersistence.Utilities;

namespace Microsoft.VisualStudio.SolutionPersistence.Model;

/// <summary>
/// Represents a solution folder in the solution model.
/// </summary>
public sealed class SolutionFolderModel : SolutionItemModel
{
    private const string CycleBreaker = "***"; // to ensure no cycles
    private string? itemRef; // folder fullPath
    private List<string>? files;
    private string name;

    internal SolutionFolderModel(SolutionModel solutionModel, string name)
        : base(solutionModel)
    {
        this.name = name;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SolutionFolderModel"/> class.
    /// Copy constructor.
    /// </summary>
    /// <param name="solutionModel">The new solution model parent.</param>
    /// <param name="folderModel">The folder model to copy.</param>
    internal SolutionFolderModel(SolutionModel solutionModel, SolutionFolderModel folderModel)
        : base(solutionModel, folderModel)
    {
        this.name = folderModel.name;
        if (folderModel.Files is not null)
        {
            this.files = [.. folderModel.Files];
        }
    }

    /// <summary>
    /// Gets the files in this solution folder.
    /// </summary>
    public IReadOnlyList<string>? Files => this.files;

    /// <summary>
    /// Adds a file to this solution folder.
    /// </summary>
    /// <param name="file">The file to add.</param>
    public void AddFile(string file)
    {
        this.files ??= [];

        if (!this.files.Contains(file))
        {
            this.files.Add(file);
        }
    }

    /// <summary>
    /// Removes a file from this solution folder.
    /// </summary>
    /// <param name="file">The file to remove.</param>
    /// <returns><see langword="true"/> if the item was found and removed.</returns>
    public bool RemoveFile(string file)
    {
        return this.files is not null && this.files.Remove(file);
    }

    public string Name
    {
        get => this.name;
        set
        {
            this.name = value;
            this.OnItemRefChanged();
        }
    }

    public override string ActualDisplayName => this.Name;

    private protected override Guid GetDefaultId()
    {
        Guid parentId = this.Parent is null ? Guid.Empty : this.Parent.Id;
        return DefaultIdGenerator.CreateIdFrom(parentId, this.Name);
    }

    public override Guid TypeId => ProjectTypeTable.SolutionFolder;

    public override string ItemRef
    {
        get
        {
            if (this.itemRef is not null)
            {
                return this.itemRef;
            }

            if (this.Parent is not null)
            {
                this.itemRef = CycleBreaker;
                string parentRef = this.Parent.ItemRef;
                if (!object.ReferenceEquals(parentRef, CycleBreaker))
                {
                    this.itemRef = $"{parentRef}{this.Name}/";
                    return this.itemRef;
                }
            }

            // no parent, or part of cycle move it on top.
            // potential duplicates in this case will be ignored/merged on save.
            this.itemRef = $"/{this.Name}/";
            return this.itemRef;
        }
    }

    private protected override void OnParentChanged()
    {
        base.OnParentChanged();
        this.OnItemRefChanged();
    }

    private protected override void OnItemRefChanged()
    {
        base.OnItemRefChanged();
        this.itemRef = null;
    }
}
