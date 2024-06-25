// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.VisualStudio.SolutionPersistence.Utilities;

namespace Microsoft.VisualStudio.SolutionPersistence.Model;

/// <summary>
/// Represents a project in the solution model.
/// </summary>
public sealed class SolutionProjectModel : SolutionItemModel
{
    private Guid typeId;
    private string type;
    private string filePath;
    private List<SolutionProjectModel>? dependencies;
    private List<ConfigurationRule>? projectConfigurationRules;

    [SetsRequiredMembers]
    internal SolutionProjectModel(SolutionModel solutionModel, string filePath, Guid typeId, string type)
        : base(solutionModel)
    {
        this.typeId = typeId;
        this.type = type;
        this.FilePath = filePath;
        this.Extension = PathExtensions.GetExtension(this.FilePath).ToString();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SolutionProjectModel"/> class.
    /// Copy constructor.
    /// </summary>
    /// <param name="solutionModel">The new solution model parent.</param>
    /// <param name="projectModel">The project model to copy.</param>
    internal SolutionProjectModel(SolutionModel solutionModel, SolutionProjectModel projectModel)
        : base(solutionModel, projectModel)
    {
        this.typeId = projectModel.TypeId;
        this.type = projectModel.Type;
        this.FilePath = projectModel.FilePath;
        this.DisplayName = projectModel.DisplayName;
        if (projectModel.dependencies is not null)
        {
            this.dependencies = [.. projectModel.dependencies];
        }

        if (projectModel.projectConfigurationRules is not null)
        {
            this.projectConfigurationRules = [.. projectModel.projectConfigurationRules];
        }
    }

    /// <summary>
    /// Gets the project configuration for the given solution configuration.
    /// </summary>
    /// <param name="solutionBuildType">The solution build type. (e.g. Debug).</param>
    /// <param name="solutionPlatform">The solution platform. (e.g. x64).</param>
    /// <returns>The project configuration for the given solution configuration.</returns>
    public (string BuildType, string Platform, bool Build, bool Deploy) GetProjectConfiguration(string solutionBuildType, string solutionPlatform)
    {
        ConfigurationRuleFollower projectTypeRules = this.Solution.ProjectTypeTable.GetProjectConfigurationRules(this);

        return (
            projectTypeRules.GetProjectBuildType(solutionBuildType, solutionPlatform) ?? solutionBuildType,
            projectTypeRules.GetProjectPlatform(solutionPlatform, solutionPlatform) ?? solutionPlatform,
            projectTypeRules.GetIsBuildable(solutionBuildType, solutionPlatform) ?? true,
            projectTypeRules.GetIsDeployable(solutionBuildType, solutionPlatform) ?? false);
    }

    public override Guid TypeId => this.typeId;

    public string Type => this.type;

    public void SetType(string type)
    {
        this.type = type;
        this.typeId = this.Solution.ProjectTypeTable.GetProjectType(type, this.Extension.AsSpan())?.ProjectTypeId ?? Guid.Empty;
    }

    public string FilePath
    {
        get => this.filePath;

        [MemberNotNull(nameof(filePath), nameof(Extension))]
        set
        {
            this.filePath = value;
            this.Extension = PathExtensions.GetExtension(this.FilePath).ToString();
            this.OnItemRefChanged();
        }
    }

    public override string ItemRef => this.FilePath;

    public string Extension { get; private set; }

    public string? DisplayName { get; set; }

    public override string ActualDisplayName => this.DisplayName ?? PathExtensions.GetStandardDisplayName(this.FilePath).ToString();

    private protected override Guid GetDefaultId()
    {
        return DefaultIdGenerator.CreateIdFrom(this.FilePath);
    }

    /// <summary>
    /// Gets the list of the dependencies of this project.
    /// </summary>
    /// <remarks>
    /// Project to project dependencies are normally stored in the project file itself,
    /// this is used for solution level dependencies.
    /// </remarks>
    public IReadOnlyList<SolutionProjectModel>? Dependencies => this.dependencies;

    /// <summary>
    /// Adds a dependency to this project.
    /// </summary>
    /// <param name="dependency">The dependency to add.</param>
    public void AddDependency(SolutionProjectModel dependency)
    {
        Argument.ThrowIfNull(dependency, nameof(dependency));
        if (!ReferenceEquals(dependency.Solution, this.Solution) || ReferenceEquals(dependency, this))
        {
            throw new ArgumentException(null, nameof(dependency));
        }

        this.dependencies ??= [];

        if (!this.dependencies.Contains(dependency))
        {
            this.dependencies.Add(dependency);
        }
    }

    /// <summary>
    /// Removes a dependency from this project.
    /// </summary>
    /// <param name="dependency">The dependency to remove.</param>
    /// <returns><see langword="true"/> if the dependency was found and removed.</returns>
    public bool RemoveDependency(SolutionProjectModel dependency)
    {
        return
            this.dependencies is not null &&
            this.dependencies.Remove(dependency);
    }

    /// <summary>
    /// Gets or sets a list of configuration rules for this project.
    /// These rules can be simplified to essential rules by calling <see cref="SolutionModel.DistillProjectConfigurations"/>.
    /// </summary>
    public IReadOnlyList<ConfigurationRule>? ProjectConfigurationRules
    {
        get => this.projectConfigurationRules;
        set => this.projectConfigurationRules = value is null ? null : [.. value];
    }

    /// <summary>
    /// Adds a configuration rule to this project.
    /// </summary>
    /// <param name="rule">The rule to add.</param>
    public void AddProjectConfigurationRule(ConfigurationRule rule)
    {
        Argument.ThrowIfNull(rule, nameof(rule));
        this.projectConfigurationRules ??= [];
        this.projectConfigurationRules.Add(rule);
    }
}
