// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.SolutionPersistence.Model;

/// <summary>
/// Represents a solution folder in the solution model.
/// </summary>
public sealed partial class SolutionFolderModel
{
    public new sealed class Builder : SolutionItemModel.Builder
    {
        private string? fullName;
        private string name;
        private List<string>? files;

        public override bool IsValid => !string.IsNullOrEmpty(this.name);

        internal Builder(SolutionModel.Builder solutionBuilder, string name)
            : base(solutionBuilder)
        {
            this.Name = name;
        }

        // Create a builder copied from an existing model folder.
        internal Builder(SolutionModel.Builder solutionBuilder, SolutionFolderModel folderModel)
            : base(solutionBuilder, folderModel)
        {
            this.name = folderModel.Name;
            this.files = folderModel.files is null ? null : new(folderModel.files);
        }

        private protected override Guid CreateId(SolutionItemModel item)
        {
            Guid parentId = item.Parent is null || this.ParentBuilder is null ? Guid.Empty : this.ParentBuilder.ResolveId(item.Parent);
            return this.SolutionBuilder.DefaultIdGenerator.CreateIdFrom(parentId, this.name);
        }

        private protected override SolutionItemModel ToModelInternal(out bool needsLinking)
        {
            SolutionFolderModel ret = new SolutionFolderModel(this.name)
            {
                files = this.files,
            };

            this.files = null;

            needsLinking = false;
            return ret;
        }

        public override string? ItemRef => this.fullName;

        public string Name
        {
            get => this.name;

            [MemberNotNull(nameof(name))]
            set
            {
                Argument.ThrowIfNull(value, nameof(value));

                // decided, it is always full name.
                this.fullName = value.Replace('\\', '/');
                int nameStart = value.LastIndexOf('/', value.Length - 2);
                if (nameStart >= 0)
                {
                    this.name = value.Substring(nameStart + 1, value.Length - nameStart - 2);
                    string parent = value.Substring(0, nameStart + 1);
                    if (parent.Length > 2)
                    {
                        this.Parent = parent;
                    }
                }
                else
                {
                    this.name = this.fullName;
                    this.Parent = null;
                }
            }
        }

        public IReadOnlyList<string>? Files => this.files;

        public void AddFile(string file)
        {
            if (string.IsNullOrEmpty(file))
            {
                return;
            }

            this.files ??= [];
            this.files.Add(file);
        }
    }
}
