// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.VisualStudio.SolutionPersistence.Serializer.Xml;
using Microsoft.VisualStudio.SolutionPersistence.Utilities;

namespace Microsoft.VisualStudio.SolutionPersistence.Model;

public sealed partial record SolutionModel
{
    public sealed class Builder
    {
        /// <summary>
        /// This is used to resolve item references and project types, that may either use guid Id's or ItemRefs.
        /// This linking occurs when the builder produces the final model.
        /// </summary>
        internal struct ItemLinker(int count)
        {
            private Dictionary<string, SolutionItemModel.Builder>? byRef = new Dictionary<string, SolutionItemModel.Builder>(count, StringComparer.OrdinalIgnoreCase);
            private Dictionary<Guid, SolutionItemModel.Builder>? byItemId = new Dictionary<Guid, SolutionItemModel.Builder>(count);
            private Dictionary<SolutionItemModel.Builder, SolutionItemModel>? itemForBuilder;

            public void ClearAll()
            {
                this.byRef = null;
                this.byItemId = null;
                this.itemForBuilder = null;
            }

            private static void RemoveValue<T>(Dictionary<T, SolutionItemModel.Builder>? dictionary, SolutionItemModel.Builder value)
                where T : notnull
            {
                if (dictionary is null)
                {
                    return;
                }

                ListBuilderStruct<T> list = new ListBuilderStruct<T>();
                foreach ((T key, SolutionItemModel.Builder itemValue) in dictionary)
                {
                    if (object.ReferenceEquals(itemValue, value))
                    {
                        list.Add(key);
                    }
                }

                foreach (T k in list)
                {
                    _ = dictionary.Remove(k);
                }
            }

            public readonly void Remove(SolutionItemModel.Builder item)
            {
                RemoveValue(this.byRef, item);
                RemoveValue(this.byItemId, item);
                _ = this.itemForBuilder?.Remove(item);
            }

            public void Add(SolutionItemModel.Builder item)
            {
                this.byItemId ??= [];
                this.byRef ??= [];
                if (item.ItemId is not null)
                {
                    this.byItemId[item.ItemId.Value] = item;
                }

                string? refStr = item.ItemRef;
                if (!refStr.IsNullOrEmpty())
                {
                    this.byRef[refStr] = item;
                }
            }

            public readonly void ClearItems()
            {
                this.itemForBuilder?.Clear();
            }

            public void Add(SolutionItemModel.Builder builder, SolutionItemModel item)
            {
                this.Add(builder);
                this.itemForBuilder ??= [];
                this.itemForBuilder.Add(builder, item);
            }

            public readonly bool TryGet(SolutionItemModel.Builder builder, out SolutionItemModel? item)
            {
                if (this.itemForBuilder is null)
                {
                    item = null;
                    return false;
                }

                return this.itemForBuilder.TryGetValue(builder, out item);
            }

            public readonly bool TryGetByRef(string refString, [NotNullWhen(true)] out SolutionItemModel.Builder? itemBuilder)
            {
                if (this.byRef is null || string.IsNullOrEmpty(refString))
                {
                    itemBuilder = null;
                    return false;
                }

                return this.byRef.TryGetValue(refString, out itemBuilder);
            }

            public readonly bool TryGet(string? id, [NotNullWhen(true)] out SolutionItemModel? item)
            {
                if (!this.TryGetBuilder(id, out SolutionItemModel.Builder? builder))
                {
                    item = null;
                    return false;
                }

                return this.TryGet(builder!, out item);
            }

            public readonly bool TryGetBuilder(string? id, [NotNullWhen(true)] out SolutionItemModel.Builder? builder)
            {
                if (Guid.TryParse(id, out Guid itemId) &&
                    this.byItemId is not null &&
                    this.byItemId.TryGetValue(itemId, out builder))
                {
                    return true;
                }

                if (!id.IsNullOrEmpty() &&
                    this.byRef is not null &&
                    this.byRef.TryGetValue(id, out builder))
                {
                    return true;
                }

                builder = null;
                return false;
            }
        }

        internal DefaultIdGenerator DefaultIdGenerator { get; } = new DefaultIdGenerator();

        internal StringTable StringTable { get; }

        private readonly List<SolutionItemModel.Builder> itemBuilders = [];
        private readonly HashSet<string> knownConfigs = [];
        private readonly HashSet<string> knownPlats = [];
        private readonly List<string> solutionBuildTypes = [];
        private readonly List<string> solutionPlatforms = [];
        private readonly Lictionary<string, SolutionPropertyBag> properties = [];

        private int nProjects;
        private int nFolders;
        private string? vsVersion;
        private string? minVsVersion;
        private string? description;
        private Guid? solutionId;

#pragma warning disable SA1401 // Fields should be private
        internal ItemLinker Linker;
#pragma warning restore SA1401 // Fields should be private

        // Ugly .SLN style ProjectConfigurationPlatforms section values.
        private IReadOnlyDictionary<string, string>? projectConfigurationPlatforms;

        internal ProjectTypeTable ProjectTypeTable { get; set; }

        public Builder(SolutionModel from, StringTable? stringTable)
        {
            Argument.ThrowIfNull(from, nameof(from));
            this.StringTable = stringTable ?? new StringTable().WithSolutionConstants();
            this.description = from.Description;
            this.Linker = new ItemLinker(from.solutionItems.Count);
            this.itemBuilders = new List<SolutionItemModel.Builder>(from.solutionItems.Count);
            this.ProjectTypeTable = from.ProjectTypeTable;
            Dictionary<SolutionItemModel, SolutionItemModel.Builder> createdItemsToBuilder = new Dictionary<SolutionItemModel, SolutionItemModel.Builder>(from.solutionItems.Count);
            foreach (SolutionItemModel item in from.solutionItems)
            {
                SolutionItemModel.Builder itemBuilder = this.AddItem(item);
                createdItemsToBuilder.Add(item, itemBuilder);
            }

            // setup parents
            foreach ((SolutionItemModel itemModel, SolutionItemModel.Builder itemBuilder) in createdItemsToBuilder)
            {
                if (itemModel.Parent is not null &&
                    createdItemsToBuilder.TryGetValue(itemModel.Parent, out SolutionItemModel.Builder? parentBuilder) &&
                    parentBuilder is SolutionFolderModel.Builder parentFolder)
                {
                    itemBuilder.SetParent(parentFolder);
                }
            }

            this.solutionPlatforms = new List<string>(from.solutionPlatforms);
            this.solutionBuildTypes = new List<string>(from.solutionBuildTypes);

            this.knownConfigs = [.. this.solutionBuildTypes];

            this.knownPlats = [.. this.solutionPlatforms];

            if (from.properties is not null)
            {
                this.properties = new Lictionary<string, SolutionPropertyBag>(from.properties.Count, StringComparer.Ordinal);
                foreach (SolutionPropertyBag propertyBag in from.properties)
                {
                    this.properties.Add(propertyBag.Id, new SolutionPropertyBag(propertyBag));
                }
            }

            this.solutionId = from.SolutionId;
            this.vsVersion = from.VsVersion;
            this.minVsVersion = from.MinVsVersion;
        }

        public Builder(StringTable? stringTable)
        {
            this.ProjectTypeTable = new ProjectTypeTable();
            this.StringTable = stringTable ?? new();
            this.Linker = new ItemLinker(0);
        }

        public IReadOnlyList<SolutionItemModel.Builder> Items => this.itemBuilders;

        public void RemoveItem(SolutionItemModel.Builder item)
        {
            _ = this.itemBuilders.Remove(item);
            this.Linker.Remove(item);
        }

        public bool TryGetItem(string id, [NotNullWhen(true)] out SolutionItemModel.Builder? itemBuilder)
        {
            return this.Linker.TryGetBuilder(id, out itemBuilder);
        }

        internal bool TryGet(string id, [NotNullWhen(true)] out SolutionItemModel? item)
        {
            return this.Linker.TryGet(id, out item);
        }

        public Guid? SolutionId { set => this.solutionId = value; }

        public string? VsVersion { set => this.vsVersion = value; }

        public string? MinVsVersion { set => this.minVsVersion = value; }

        public string? Description { set => this.description = value; }

        public bool Corrupted { get; set; }

        public bool HasProjects => this.nProjects > 0;

        public IReadOnlyList<ProjectType>? ProjectTypes
        {
            get => this.ProjectTypeTable?.ProjectTypes;
            set => this.ProjectTypeTable = new ProjectTypeTable([.. value], logger: null);
        }

        public SolutionItemModel.Builder AddItem(SolutionItemModel itemModel)
        {
            return itemModel switch
            {
                SolutionFolderModel folderModel => this.AddFolder(folderModel),
                SolutionProjectModel projectModel => this.AddProject(projectModel),
                _ => throw new ArgumentException(null, nameof(itemModel)),
            };
        }

        public SolutionFolderModel.Builder AddFolder(SolutionFolderModel folderModel)
        {
            Argument.ThrowIfNull(folderModel, nameof(folderModel));
            return this.AddFolder(new SolutionFolderModel.Builder(this, folderModel));
        }

        public SolutionFolderModel.Builder AddFolder(string name)
        {
            return this.AddFolder(new SolutionFolderModel.Builder(this, name));
        }

        private SolutionFolderModel.Builder AddFolder(SolutionFolderModel.Builder folderBuilder)
        {
            this.nFolders++;

            if (this.itemBuilders.Contains(folderBuilder))
            {
                throw new ArgumentException("The folder " + folderBuilder.Name + " was already added.");
            }

            this.itemBuilders.Add(folderBuilder);
            this.Linker.Add(folderBuilder);
            return folderBuilder;
        }

        public SolutionProjectModel.Builder AddProject(SolutionProjectModel projectModel)
        {
            Argument.ThrowIfNull(projectModel, nameof(projectModel));
            return this.AddProject(new SolutionProjectModel.Builder(this, projectModel));
        }

        public SolutionProjectModel.Builder AddProject(string filePath)
        {
            return this.AddProject(new SolutionProjectModel.Builder(this, filePath));
        }

        private SolutionProjectModel.Builder AddProject(SolutionProjectModel.Builder projectBuilder)
        {
            this.nProjects++;

            if (this.itemBuilders.Contains(projectBuilder))
            {
                throw new ArgumentException("The project " + projectBuilder.FilePath + " was already added.");
            }

            this.itemBuilders.Add(projectBuilder);
            this.Linker.Add(projectBuilder);
            return projectBuilder;
        }

        public IReadOnlyList<string> BuildTypes => this.solutionBuildTypes;

        public IReadOnlyList<string> Platforms => this.solutionPlatforms;

        public void AddBuildType(string buildType)
        {
            buildType = this.StringTable.GetString(buildType);
            if (!string.IsNullOrEmpty(buildType) && this.knownConfigs.Add(buildType))
            {
                this.solutionBuildTypes.Add(buildType);
            }
        }

        public void AddPlatform(string platform)
        {
            platform = this.StringTable.GetString(platform);
            if (!string.IsNullOrEmpty(platform) && this.knownPlats.Add(platform))
            {
                this.solutionPlatforms.Add(platform);
            }
        }

        public void AddSolutionConfiguration(string slnConfiguration)
        {
            if (ModelHelper.TrySplitFullConfiguration(slnConfiguration, out StringSpan buildType, out StringSpan platform))
            {
                this.AddBuildType(this.StringTable.GetString(BuildTypeNames.ToStringKnown(buildType)));
                this.AddPlatform(this.StringTable.GetString(PlatformNames.ToStringKnown(platform)));
            }
        }

        public SolutionPropertyBag? TryGetProperties(string id)
        {
            return this.properties.TryGetValue(id, out SolutionPropertyBag? map) ? map : null;
        }

        public SolutionPropertyBag EnsureProperties(string id, PropertiesScope scope = PropertiesScope.PreLoad)
        {
            return this.TryGetProperties(id) ?? this.AddProperties(new SolutionPropertyBag(id, scope));
        }

        // Can be used to parse the SLN style list of project configuration platforms.
        public void SetProjectConfigurationPlatforms(IReadOnlyDictionary<string, string> projectConfigurationPlatforms)
        {
            this.projectConfigurationPlatforms = projectConfigurationPlatforms;
        }

        [return: NotNullIfNotNull(nameof(properties))]
        public SolutionPropertyBag? AddProperties(SolutionPropertyBag? properties)
        {
            if (properties is null)
            {
                return null;
            }

            SolutionPropertyBag? existingPropertyBag = this.TryGetProperties(properties.Id);
            if (existingPropertyBag is not null)
            {
                existingPropertyBag.AddRange(properties);
                return existingPropertyBag;
            }
            else
            {
                this.properties.Add(properties.Id, properties);
                return properties;
            }
        }

        public bool RemoveProperties(string id)
        {
            return this.properties.Remove(id);
        }

        private SolutionFolderModel.Builder EnsureFolder(string fullFolderName, SolutionModel result, List<(SolutionItemModel.Builder Builder, SolutionItemModel Item)> toLink)
        {
            // Look for existing folder.
            if (this.Linker.TryGetByRef(fullFolderName, out SolutionItemModel.Builder? foundItemBuilder) &&
                foundItemBuilder is SolutionFolderModel.Builder foundFolderBuilder)
            {
                return foundFolderBuilder;
            }

            SolutionFolderModel.Builder folderBuilder = this.AddFolder(fullFolderName);
            this.Linker.Add(folderBuilder);
            if (folderBuilder.Parent is not null)
            {
                SolutionFolderModel.Builder parent = this.EnsureFolder(folderBuilder.Parent, result, toLink);
                folderBuilder.SetParent(parent);
            }

            SolutionFolderModel newItem = (SolutionFolderModel)folderBuilder.ToModel(out bool needLink);
            result.solutionItems.Add(newItem);
            result.solutionFolders.Add(newItem);
            if (needLink)
            {
                toLink.Add((folderBuilder, newItem));
            }

            return folderBuilder;
        }

        public SolutionModel ToModel(ISerializerModelExtension serializerExtension)
        {
            List<SolutionItemModel> itemList = new List<SolutionItemModel>(this.itemBuilders.Count);
            List<SolutionProjectModel> prjList = new List<SolutionProjectModel>(this.nProjects);
            List<SolutionFolderModel> folderList = new List<SolutionFolderModel>(this.nFolders);
            List<string> buildTypes = this.solutionBuildTypes;
            List<string> platforms = this.solutionPlatforms;

            SolutionModel solutionModel = new SolutionModel(
                serializerExtension: serializerExtension,
                buildTypes: buildTypes,
                platforms: platforms,
                items: itemList,
                projects: prjList,
                folders: folderList,
                projectTypes: this.ProjectTypeTable)
            {
                SolutionId = this.solutionId,
                VsVersion = this.vsVersion,
                MinVsVersion = this.minVsVersion,
                Description = this.description,
            };

            if (this.properties.Count > 0)
            {
                solutionModel.properties = new(this.properties.Count);
                foreach ((string _, SolutionPropertyBag propertyBag) in this.properties)
                {
                    propertyBag.Freeze();
                    solutionModel.properties.Add(propertyBag);
                }
            }

            this.Linker.ClearItems();
            List<(SolutionItemModel.Builder Builder, SolutionItemModel Item)> toLink =
                new List<(SolutionItemModel.Builder Builder, SolutionItemModel Item)>(this.itemBuilders.Count);
            HashSet<string> ensureFolders = new HashSet<string>(this.itemBuilders.Count, StringComparer.OrdinalIgnoreCase);

            foreach (SolutionItemModel.Builder itemBuilder in this.itemBuilders)
            {
                SolutionItemModel item = itemBuilder.ToModel(out bool needsLinking);
                if (needsLinking)
                {
                    toLink.Add((itemBuilder, item));
                }

                this.Linker.Add(itemBuilder, item);

                if (item is SolutionProjectModel prj)
                {
                    solutionModel.solutionProjects.Add(prj);
                }
                else if (item is SolutionFolderModel fold)
                {
                    solutionModel.solutionFolders.Add(fold);
                }
                else
                {
                    throw new NotImplementedException();
                }

                if (itemBuilder.IsParentFolderRef && !itemBuilder.Parent.IsNullOrEmpty())
                {
                    _ = ensureFolders.Add(itemBuilder.Parent);
                }

                solutionModel.solutionItems.Add(item);
            }

            // setup folders in case some of parent folders are missing while we use folder refs (aka "/rootFolder/Folder/")
            // normally if folder refs mode is used, all folder names should be full names and all folders would be present (unless we decide to skip empty folders)
            foreach (string ensureFolder in ensureFolders)
            {
                _ = this.EnsureFolder(ensureFolder, solutionModel, toLink);
            }

            foreach ((SolutionItemModel.Builder builder, SolutionItemModel item) in toLink)
            {
                builder.ResolveParentAndType(item);
            }

            // ItemIds need folder parents resolved.
            foreach ((SolutionItemModel.Builder builder, SolutionItemModel item) in toLink)
            {
                _ = builder.ResolveId(item);
            }

            // Dependencies need ItemIds resolved.
            foreach ((SolutionItemModel.Builder builder, SolutionItemModel item) in toLink)
            {
                builder.ResolveDependencies(item);
            }

            // TODO: This is .sln specific
            if (this.projectConfigurationPlatforms?.Count > 0)
            {
                SolutionConfigurationMap cfgMap = new SolutionConfigurationMap(solutionModel, this);

                // Converts the .sln style project configuration platforms into a mappings for each configuration.
                foreach ((string projectKey, string projectValue) in this.projectConfigurationPlatforms)
                {
                    cfgMap.ParseProjectConfigLine(projectKey, projectValue);
                }

                foreach (SolutionProjectModel projectModel in solutionModel.solutionProjects)
                {
                    // Converts cached mappings into simpler rules.
                    ConfigurationRule[]? projectConfigurationRules = cfgMap.CreateProjectRules(projectModel);
                    projectModel.SetProjectConfigurationRules(projectConfigurationRules);
                }
            }

            return solutionModel;
        }
    }
}
