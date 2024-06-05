// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.VisualStudio.SolutionPersistence.Utilities;

namespace Microsoft.VisualStudio.SolutionPersistence.Model;

/// <summary>
/// Represents an item in the solution model, either a project or a solution folder.
/// </summary>
public abstract partial class SolutionItemModel
{
    public abstract class Builder
    {
        private List<SolutionPropertyBag>? properties;
        private Dictionary<string, SolutionPropertyBag>? propertiesIndex;
        private SolutionFolderModel.Builder? parentBuilder;

        private protected SolutionModel.Builder SolutionBuilder { get; }

        private protected Builder(SolutionModel.Builder solutionBuilder)
        {
            this.SolutionBuilder = solutionBuilder;
        }

        private protected Builder(SolutionModel.Builder solutionBuilder, SolutionItemModel itemModel)
            : this(solutionBuilder)
        {
            this.ItemId = itemModel.Id == Guid.Empty ? null : itemModel.Id;
            foreach (SolutionPropertyBag propertyBag in itemModel.Properties.GetStructEnumerable())
            {
                _ = this.AddProperties(propertyBag);
            }
        }

        public abstract string? ItemRef { get; }

        public Guid? ItemId { get; set; }

        public string? Parent { get; set; }

        public bool IsParentFolderRef => this.ParentBuilder is null && this.Parent.IsFullFolderName();

        public void SetParent(SolutionFolderModel.Builder? parent) => this.parentBuilder = parent;

        public Builder? ParentBuilder => this.parentBuilder;

        public abstract bool IsValid { get; }

        [return: NotNullIfNotNull(nameof(map))]
        public SolutionPropertyBag? AddProperties(SolutionPropertyBag? map)
        {
            if (map is null)
            {
                return null;
            }

            this.propertiesIndex ??= [];
            if (this.propertiesIndex.TryGetValue(map.Id!, out SolutionPropertyBag? alreadyMap))
            {
                alreadyMap.AddRange(map);
                return alreadyMap;
            }
            else
            {
                (this.properties ??= []).Add(map);
                this.propertiesIndex.Add(map.Id!, map);
                return map;
            }
        }

        private protected virtual void ResolveParentAndTypeInternal(SolutionItemModel item)
        {
        }

        private protected abstract Guid CreateId(SolutionItemModel item);

        internal void ResolveParentAndType(SolutionItemModel item)
        {
            this.ResolveParentAndTypeInternal(item);
            if (this.parentBuilder is null &&
                this.SolutionBuilder.Linker.TryGetBuilder(this.Parent, out Builder? parentBuilderItem) &&
                parentBuilderItem is SolutionFolderModel.Builder parentBuilderFolder)
            {
                this.parentBuilder = parentBuilderFolder;
            }

            if (this.parentBuilder is not null &&
                this.SolutionBuilder.Linker.TryGet(this.parentBuilder, out SolutionItemModel? parentItem) &&
                parentItem is SolutionFolderModel parentFolder)
            {
                item.Parent = parentFolder;
            }
        }

        internal virtual void ResolveDependencies(SolutionItemModel item)
        {
        }

        public Guid ResolveId(SolutionItemModel item)
        {
            Argument.ThrowIfNull(item, nameof(item));
            if (item.Id == Guid.Empty)
            {
                item.Id = this.CreateId(item);
            }

            return item.Id;
        }

        private protected abstract SolutionItemModel ToModelInternal(out bool needsLinking);

        internal SolutionItemModel ToModel(out bool needsLinking)
        {
            SolutionItemModel ret = this.ToModelInternal(out needsLinking);

            if (this.ItemId.HasValue)
            {
                ret.Id = this.ItemId.Value;
            }
            else
            {
                ret.Id = Guid.Empty;
                needsLinking = true;
            }

            int propertyBagCount = this.properties?.Count ?? 0;
            if (propertyBagCount > 0)
            {
                ret.properties = new List<SolutionPropertyBag>(propertyBagCount);
                if (this.properties?.Count > 0)
                {
                    foreach (SolutionPropertyBag propertyBag in this.properties)
                    {
                        propertyBag.Freeze();
                        ret.properties.Add(propertyBag);
                    }
                }
            }

            this.properties = null;
            this.propertiesIndex = null;

            needsLinking |= this.parentBuilder is not null || this.Parent is not null;
            return ret;
        }
    }
}
