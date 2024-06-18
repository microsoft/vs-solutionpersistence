// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq;
using Microsoft.VisualStudio.SolutionPersistence.Serializer.Xml;

namespace Microsoft.VisualStudio.SolutionPersistence.Model;

/// <summary>
/// Represents a solution.
/// This contains a list of projects and folders and the information
/// required to build the solution in different configurations.
/// </summary>
public sealed class SolutionModel : PropertyContainerModel
{
    private readonly Dictionary<Guid, SolutionItemModel> solutionItemsById;
    private readonly List<SolutionItemModel> solutionItems;
    private readonly List<SolutionProjectModel> solutionProjects;
    private readonly List<SolutionFolderModel> solutionFolders;
    private readonly List<string> solutionBuildTypes;
    private readonly List<string> solutionPlatforms;
    private readonly List<ProjectType> projectTypes;
    private ProjectTypeTable? projectTypeTable;

    public SolutionModel()
    {
        this.solutionItemsById = [];
        this.solutionItems = [];
        this.solutionProjects = [];
        this.solutionFolders = [];
        this.solutionBuildTypes = [];
        this.solutionPlatforms = [];
        this.projectTypes = [];
        this.StringTable = new StringTable().WithSolutionConstants();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SolutionModel"/> class.
    /// Copy constructor.
    /// </summary>
    /// <param name="solutionModel">Instance of the <see cref="SolutionModel"/> to copy.</param>
    public SolutionModel(SolutionModel solutionModel)
        : base(solutionModel ?? throw new ArgumentNullException(nameof(solutionModel)))
    {
        int itemCount = solutionModel.solutionItems.Count;
        int folderCount = solutionModel.solutionItems.Count(x => x is SolutionFolderModel);
        this.solutionItems = new List<SolutionItemModel>(itemCount);
        this.solutionItemsById = new Dictionary<Guid, SolutionItemModel>(itemCount);
        this.solutionFolders = new List<SolutionFolderModel>(folderCount);
        this.solutionProjects = new List<SolutionProjectModel>(itemCount - folderCount);
        foreach (SolutionItemModel item in solutionModel.solutionItems)
        {
            SolutionItemModel newItem = item switch
            {
                SolutionFolderModel folder => new SolutionFolderModel(this, folder),
                SolutionProjectModel project => new SolutionProjectModel(this, project),
                _ => throw new InvalidOperationException(),
            };

            this.solutionItems.Add(newItem);
            this.solutionFolders.AddIfNotNull(newItem as SolutionFolderModel);
            this.solutionProjects.AddIfNotNull(newItem as SolutionProjectModel);
            this.solutionItemsById[newItem.Id] = newItem;
        }

        // Replace the shallow-parent models with the new folders.
        foreach (SolutionItemModel item in this.solutionItems)
        {
            if (item.Parent is not null)
            {
                item.Parent =
                    this.FindItemByItemRef(item.Parent.ItemRef) as SolutionFolderModel ??
                    throw new InvalidOperationException();
            }
        }

        this.MinVsVersion = solutionModel.MinVsVersion;
        this.VsVersion = solutionModel.VsVersion;
        this.SolutionId = solutionModel.SolutionId;
        this.Description = solutionModel.Description;
        this.solutionBuildTypes = [.. solutionModel.solutionBuildTypes];
        this.solutionPlatforms = [.. solutionModel.solutionPlatforms];
        this.projectTypes = [.. solutionModel.projectTypes];
        this.StringTable = solutionModel.StringTable;
    }

    internal ProjectTypeTable ProjectTypeTable => this.projectTypeTable ??= new ProjectTypeTable(this.projectTypes, logger: null);

    public StringTable StringTable { get; set; }

    public ISerializerModelExtension? SerializerExtension { get; set; }

    // CONSIDER: Move these to VS property bag.
    #region Visual Studio Properties

    public Guid? SolutionId { get; set; }

    public Guid DefaultId { get; set; }

    // VisualStudioVersion
    public string? VsVersion { get; set; }

    // MinimumVisualStudioVersion
    public string? MinVsVersion { get; set; }

    #endregion

    public string? Description { get; set; }

    public IReadOnlyList<SolutionItemModel> SolutionItems => this.solutionItems;

    public IReadOnlyList<SolutionProjectModel> SolutionProjects => this.solutionProjects;

    public IReadOnlyList<SolutionFolderModel> SolutionFolders => this.solutionFolders;

    /// <summary>
    /// Gets the list of build types in the solution. (e.g Debug/Release).
    /// </summary>
    public IReadOnlyList<string> BuildTypes => this.solutionBuildTypes;

    /// <summary>
    /// Gets the list of platforms in the solution. (e.g. x64/Any CPU).
    /// </summary>
    public IReadOnlyList<string> Platforms => this.solutionPlatforms;

    internal bool IsConfigurationImplicit()
    {
        return
            this.IsBuildTypeImplicit() &&
            this.IsPlatformImplicit() &&
            this.ProjectTypeTable.ProjectTypes.Count == 0;
    }

    internal bool IsBuildTypeImplicit()
    {
        // Has 0 build types, or just Debug/Release.
        return
            this.BuildTypes.Count == 0 ||
            (this.BuildTypes.Count == 2 &&
            this.BuildTypes.Contains(BuildTypeNames.Debug) &&
            this.BuildTypes.Contains(BuildTypeNames.Release));
    }

    internal bool IsPlatformImplicit()
    {
        return
            this.Platforms.Count == 0 ||
            (this.Platforms.Count == 1 &&
            this.Platforms[0] == PlatformNames.AnySpaceCPU);
    }

    /// <summary>
    /// Gets or sets the list of project types in the solution.
    /// </summary>
    /// <remarks>
    /// These can be defined to provide information about a project type used in the solution.
    /// It can be associated with a file extension or a friendly name.
    /// It contains the project type id and and default configuration mapping rules.
    /// </remarks>
    public IReadOnlyList<ProjectType> ProjectTypes
    {
        get => this.projectTypes;
        set
        {
            this.projectTypes.Clear();
            this.projectTypes.AddRange(value);
            this.projectTypeTable = null;
        }
    }

    /// <summary>
    /// Adds a solution folder to the solution.
    /// </summary>
    /// <param name="name">
    /// The full path of the solution folder.
    /// If parent folders do not exist, they will be created.
    /// </param>
    /// <returns>The model for the new folder.</returns>
    public SolutionFolderModel AddFolder(string name)
    {
        Argument.ThrowIfNull(name, nameof(name));

        // Check if the folder was already created by a child folder.
        if (this.FindItemByItemRef(name) is SolutionFolderModel existingFolder)
        {
            return existingFolder;
        }

        // Process the folder name
        SolutionFolderModel? parentFolder;
        StringSpan folderPath = name.AsSpan().TrimEnd('/');
        int lastSlash = folderPath.LastIndexOf('/');
        if (lastSlash > 0)
        {
            string parentItemRef = folderPath.Slice(0, lastSlash + 1).ToString();
            parentFolder =
                this.FindItemByItemRef(parentItemRef) as SolutionFolderModel ??
                this.AddFolder(parentItemRef.ToString());
            name = this.StringTable.GetString(folderPath.Slice(lastSlash + 1));
        }
        else
        {
            parentFolder = null;
            name = this.StringTable.GetString(folderPath.Trim('/'));
        }

        SolutionFolderModel folder = new SolutionFolderModel(this, name)
        {
            Parent = parentFolder,
        };

        this.solutionFolders.Add(folder);
        this.solutionItems.Add(folder);

        // Ensure the project type is in the project type table, if it is not already.
        this.solutionItemsById[folder.Id] = folder;

        return folder;
    }

    /// <summary>
    /// Adds a project to the solution.
    /// </summary>
    /// <param name="filePath">The relative path to the project.</param>
    /// <param name="projectTypeName">
    /// The project type name of the project.
    /// This can be null if the project type can be determined from the project's file extension.
    /// </param>
    /// <returns>The model for the new project.</returns>
    public SolutionProjectModel AddProject(string filePath, string? projectTypeName)
    {
        ProjectType projectType =
            this.ProjectTypeTable.GetProjectType(projectTypeName, Path.GetExtension(filePath.AsSpan())) ??
            throw new ArgumentException(null, nameof(projectTypeName));

        return this.AddProject(filePath, projectType.ProjectTypeId, projectTypeName ?? string.Empty);
    }

    /// <summary>
    /// Add a project to the solution.
    /// </summary>
    /// <param name="filePath">The relative path to the project.</param>
    /// <param name="projectTypeId">The project type id of the project.</param>
    /// <returns>The model for the new project.</returns>
    public SolutionProjectModel AddProject(string filePath, Guid projectTypeId)
    {
        string projectTypeName = this.ProjectTypeTable.TryGetProjectType(projectTypeId, out ProjectType? projectType) ?
            projectType.Name ?? projectTypeId.ToString() :
            projectTypeId.ToString();

        return this.AddProject(filePath, projectTypeId, projectTypeName);
    }

    private SolutionProjectModel AddProject(string filePath, Guid projectTypeId, string projectType)
    {
        SolutionProjectModel project = new SolutionProjectModel(this, filePath, projectTypeId, projectType);

        this.solutionProjects.Add(project);
        this.solutionItems.Add(project);

        // Ensure the project type is in the project type table, if it is not already.
        this.solutionItemsById[project.Id] = project;

        return project;
    }

    /// <summary>
    /// Remove a solution folder from the solution model.
    /// </summary>
    /// <param name="folder">The folder to remove.</param>
    /// <returns><see langword="true"/> if the folder was found and removed.</returns>
    public bool RemoveFolder(SolutionFolderModel folder)
    {
        Argument.ThrowIfNull(folder, nameof(folder));
        _ = this.solutionFolders.Remove(folder);

        // Remove any children of this folder.
        foreach (SolutionItemModel existingItem in this.SolutionItems)
        {
            if (existingItem.Parent == folder)
            {
                existingItem.Parent = folder.Parent;
            }
        }

        return this.RemoveItem(folder);
    }

    /// <summary>
    /// Remove a project from the solution model.
    /// </summary>
    /// <param name="project">The item to remove.</param>
    /// <returns><see langword="true"/> if the project was found and removed.</returns>
    public bool RemoveProject(SolutionProjectModel project)
    {
        Argument.ThrowIfNull(project, nameof(project));
        _ = this.solutionProjects.Remove(project);

        // Remove any dependencies to this project.
        foreach (SolutionProjectModel existingProject in this.SolutionProjects)
        {
            _ = existingProject.RemoveDependency(project);
        }

        return this.RemoveItem(project);
    }

    private bool RemoveItem(SolutionItemModel item)
    {
        _ = this.solutionItemsById.Remove(item.Id);
        return this.solutionItems.Remove(item);
    }

    /// <summary>
    /// Adds a build type to the solution.
    /// </summary>
    /// <param name="buildType">The build type to add.</param>
    public void AddBuildType(string buildType)
    {
        buildType = this.StringTable.GetString(buildType);

        if (!this.solutionBuildTypes.Contains(buildType))
        {
            this.solutionBuildTypes.Add(buildType);
        }
    }

    /// <summary>
    /// Removes a build type from the solution.
    /// </summary>
    /// <param name="buildType">The build type to remove.</param>
    /// <returns><see langword="true"/> if the build type was found and removed.</returns>
    public bool RemoveBuildType(string buildType)
    {
        return this.solutionBuildTypes.Remove(buildType);
    }

    /// <summary>
    /// Adds a platform to the solution.
    /// </summary>
    /// <param name="platform">The platform to add.</param>
    public void AddPlatform(string platform)
    {
        platform = this.StringTable.GetString(platform);

        if (!this.solutionPlatforms.Contains(platform))
        {
            this.solutionPlatforms.Add(platform);
        }
    }

    /// <summary>
    /// Removes a platform from the solution.
    /// </summary>
    /// <param name="platform">The platform to remove.</param>
    /// <returns><see langword="true"/> if the platform was found and removed.</returns>
    public bool RemovePlatform(string platform)
    {
        return this.solutionPlatforms.Remove(platform);
    }

    /// <summary>
    /// Find a solution folder or project by id.
    /// </summary>
    /// <param name="id">The id of the item to look for.</param>
    /// <returns>The item if found.</returns>
    public SolutionItemModel? FindItemById(Guid id)
    {
        return this.solutionItemsById.TryGetValue(id, out SolutionItemModel? item) ? item : null;
    }

    /// <summary>
    /// Find a solution folder or project by item ref.
    /// </summary>
    /// <param name="itemRef">The item ref of the item to look for.</param>
    /// <returns>The item if found.</returns>
    public SolutionItemModel? FindItemByItemRef(string itemRef)
    {
        return ModelHelper.FindByItemRef(this.solutionItems, itemRef);
    }

    internal void OnUpdateId(SolutionItemModel solutionItemModel, Guid? oldId)
    {
        if (oldId is not null)
        {
            _ = this.solutionItemsById.Remove(oldId.Value);
        }

        this.solutionItemsById[solutionItemModel.Id] = solutionItemModel;
    }

    /// <summary>
    /// Regenerates all of the project configuration rules. If rules are added
    /// to project types, or possible redundant rules are added to projects this
    /// can be called to recalculate the rules.
    /// </summary>
    public void DistillProjectConfigurations()
    {
        SolutionConfigurationMap cfgMap = new SolutionConfigurationMap(this);

        // Load all of the current rules for the project and recalculate a new
        // set of configuration rules.
        cfgMap.DistillProjectConfigurations();
    }
}
