// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.SolutionPersistence.Model;

/// <summary>
/// Represents an item in the solution model, either a project or a solution folder.
/// </summary>
public abstract class SolutionItemModel : PropertyContainerModel
{
    private SolutionFolderModel? parent;
    private Guid? id;
    private Guid? defaultId;

    private protected SolutionItemModel(SolutionModel solutionModel)
    {
        Argument.ThrowIfNull(solutionModel, nameof(solutionModel));
        this.Solution = solutionModel;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SolutionItemModel"/> class.
    /// Copy constructor. This does a shallow copy of the Parent.
    /// </summary>
    /// <param name="solutionModel">The new solution model parent.</param>
    /// <param name="itemModel">The item model to copy.</param>
    private protected SolutionItemModel(SolutionModel solutionModel, SolutionItemModel itemModel)
        : base(itemModel)
    {
        this.Solution = solutionModel;
        this.id = itemModel.id;
        this.defaultId = itemModel.defaultId;

        // This is a shallow copy of the parent, it needs to be swapped out to finish the deep copy.
        // But we can't find the new parent until all copy constructors have been called.
        this.parent = itemModel.Parent;
    }

    /// <summary>
    /// Gets the solution model that contains this item.
    /// </summary>
    public SolutionModel Solution { get; }

    /// <summary>
    /// Gets or sets the parent solution folder.
    /// </summary>
    public SolutionFolderModel? Parent
    {
        get => this.parent;
        set
        {
            if (!ReferenceEquals(this.parent, value))
            {
                this.parent = value;
                this.OnParentChanged();
            }
        }
    }

    /// <summary>
    /// Gets or sets the unique Id of the item within the solution.
    /// </summary>
    public Guid Id
    {
        get => this.id ?? this.DefaultId;

        set
        {
            if (value != (this.id ?? this.defaultId))
            {
                if (this.Solution.FindItemById(value) is not null)
                {
                    throw new ArgumentException(@"An item with the same Id already exists in the solution.", nameof(value));
                }

                Guid? oldId = this.id ?? this.defaultId;
                this.id = value == this.DefaultId ? null : value.NullIfEmpty();
                this.Solution.OnUpdateId(this, oldId);
            }
        }
    }

    /// <summary>
    /// Gets a value indicating whether the Id is the default Id generated from the ItemRef.
    /// </summary>
    public bool IsDefaultId => this.id is null;

    /// <summary>
    /// Gets the display name of the item. If there is a filename it will be used, otherwise the actual display name.
    /// </summary>
    public abstract string ActualDisplayName { get; }

    /// <summary>
    /// Gets a unique reference to the item in the solution.
    /// This is designed as a replacement to Id and provides a human readable reference to the item.
    /// </summary>
    public abstract string ItemRef { get; }

    /// <summary>
    /// Gets the project type id of the item.
    /// </summary>
    public abstract Guid TypeId { get; }

    private Guid DefaultId => this.defaultId ??= this.GetDefaultId();

    private protected abstract Guid GetDefaultId();

    private protected virtual void OnParentChanged()
    {
    }

    private protected virtual void OnItemRefChanged()
    {
        this.defaultId = null;
        if (this.id is null)
        {
            this.Id = Guid.Empty;
        }
    }
}
