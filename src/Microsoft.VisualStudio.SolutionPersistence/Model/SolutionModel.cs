// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq;

namespace Microsoft.VisualStudio.SolutionPersistence.Model;

public sealed partial record SolutionModel
{
    private readonly List<SolutionItemModel> solutionItems;
    private readonly List<SolutionProjectModel> solutionProjects;
    private readonly List<SolutionFolderModel> solutionFolders;
    private readonly List<string> solutionBuildTypes;
    private readonly List<string> solutionPlatforms;
    private List<SolutionPropertyBag>? properties;

    internal ProjectTypeTable ProjectTypeTable { get; }

    [SetsRequiredMembers]
    private SolutionModel(
        ISerializerModelExtension serializerExtension,
        List<string> buildTypes,
        List<string> platforms,
        List<SolutionItemModel> items,
        List<SolutionProjectModel> projects,
        List<SolutionFolderModel> folders,
        ProjectTypeTable projectTypes)
    {
        this.SerializerExtension = serializerExtension;
        this.solutionBuildTypes = buildTypes;
        this.solutionPlatforms = platforms;
        this.solutionItems = items;
        this.solutionProjects = projects;
        this.solutionFolders = folders;
        this.ProjectTypeTable = projectTypes;
    }

    public required ISerializerModelExtension SerializerExtension { get; init; }

    #region Move to Extras!

    public Guid? SolutionId { get; init; }

    public required Guid DefaultId { get; init; }

    // VisualStudioVersion
    public string? VsVersion { get; init; }

    // MinimumVisualStudioVersion
    public string? MinVsVersion { get; init; }

    public string? Description { get; init; }

    #endregion

    public IReadOnlyList<SolutionItemModel> SolutionItems => this.solutionItems;

    public IReadOnlyList<SolutionProjectModel> SolutionProjects => this.solutionProjects;

    public IReadOnlyList<SolutionFolderModel> SolutionFolders => this.solutionFolders;

    public IReadOnlyList<SolutionPropertyBag>? Properties => this.properties;

    public IReadOnlyList<string> BuildTypes => this.solutionBuildTypes;

    public IReadOnlyList<string> Platforms => this.solutionPlatforms;

    public bool IsConfigurationImplicit()
    {
        return
            this.IsBuildTypeImplicit() &&
            this.IsPlatformImplicit() &&
            this.ProjectTypeTable.ProjectTypes.Count == 0;
    }

    public bool IsBuildTypeImplicit()
    {
        // Has 0 build types, or just Debug/Release.
        return
            this.BuildTypes.Count == 0 ||
            (this.BuildTypes.Count == 2 &&
            this.BuildTypes.Contains(BuildTypeNames.Debug) &&
            this.BuildTypes.Contains(BuildTypeNames.Release));
    }

    public bool IsPlatformImplicit()
    {
        return
            this.Platforms.Count == 0 ||
            (this.Platforms.Count == 1 &&
            this.Platforms[0] == PlatformNames.AnySpaceCPU);
    }

    public IReadOnlyList<ProjectType> ProjectTypes => this.ProjectTypeTable.ProjectTypes;
}
