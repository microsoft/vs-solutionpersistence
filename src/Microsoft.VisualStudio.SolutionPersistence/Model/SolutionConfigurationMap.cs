// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.SolutionPersistence.Model;

/// <summary>
/// Helper to convert full list of solution to project configuration mappings to model rules and vice versa.
/// </summary>
internal sealed partial class SolutionConfigurationMap
{
    // TODO Move constants to SlnFileV12 parser.
    public const string ActiveCfgSuffix = ".ActiveCfg";
    public const string BuildSuffix = ".Build.0";
    public const string DeploySuffix = ".Deploy.0";

    private readonly SolutionModel solutionModel;
    private readonly SolutionModel.Builder? solutionBuilder;
    private readonly Dictionary<string, int> buildTypesIndex = [];
    private readonly Dictionary<string, int> platformsIndex = [];
    private readonly Dictionary<string, SolutionConfigIndex> byFullConfiguration = [];
    private readonly Dictionary<string, (string BuildType, string Platform)> splitCfgPlatCache = [];

    private readonly Dictionary<SolutionProjectModel, SolutionToProjectMappings> perProjectCurrent = [];

    private readonly int matrixSize;

    public SolutionConfigurationMap(SolutionModel solutionModel)
    {
        this.solutionModel = solutionModel;
        for (int i = 0; i < solutionModel.BuildTypes.Count; i++)
        {
            this.buildTypesIndex.Add(solutionModel.BuildTypes[i], i);
        }

        for (int i = 0; i < solutionModel.Platforms.Count; i++)
        {
            this.platformsIndex.Add(solutionModel.Platforms[i].Canonical(), i);
        }

        this.matrixSize = this.BuildTypesCount * this.PlatformsCount;
    }

    internal SolutionConfigurationMap(SolutionModel solutionModel, SolutionModel.Builder solutionBuilder)
        : this(solutionModel)
    {
        this.solutionBuilder = solutionBuilder;
    }

    private SolutionConfigIndex ToIndex(int iBuildType, int iPlatform) => new SolutionConfigIndex(this, iBuildType, iPlatform);

    public int GetBuildTypeIndex(string buildType)
    {
        return !string.IsNullOrEmpty(buildType) && this.buildTypesIndex.TryGetValue(buildType, out int index) ? index : ScopedRules.All;
    }

    public int GetPlatformIndex(string platform)
    {
        return !string.IsNullOrEmpty(platform) && this.platformsIndex.TryGetValue(platform.Canonical(), out int index) ? index : ScopedRules.All;
    }

    // This should only be called from ConfigIndex
    private string BuildTypeFromIndex(SolutionConfigIndex index)
    {
        if (index.MatrixIndex < 0 || index.MatrixIndex >= this.matrixSize)
        {
            throw new ArgumentOutOfRangeException(nameof(index), "Bug, invalid configuration index");
        }

        int config = index.MatrixIndex / this.PlatformsCount;
        return this.solutionModel.BuildTypes[config];
    }

    // This should only be called from ConfigIndex
    private string PlatformFromIndex(SolutionConfigIndex index)
    {
        if (index.MatrixIndex < 0 || index.MatrixIndex >= this.matrixSize)
        {
            throw new ArgumentOutOfRangeException(nameof(index), "Bug, invalid configuration index");
        }

        int plat = index.MatrixIndex % this.PlatformsCount;
        return this.solutionModel.Platforms[plat];
    }

    public int BuildTypesCount => this.buildTypesIndex.Count;

    public int PlatformsCount => this.platformsIndex.Count;

    public SolutionConfigIndex GetConfigIndex(string fullConfiguration)
    {
        if (string.IsNullOrEmpty(fullConfiguration))
        {
            return new SolutionConfigIndex();
        }

        if (this.byFullConfiguration.TryGetValue(fullConfiguration, out SolutionConfigIndex index))
        {
            return index;
        }

        if (this.TrySplitFullConfigurationCached(fullConfiguration, out string? buildType, out string? platform))
        {
            index = new SolutionConfigIndex(this, buildType, platform);
        }

        this.byFullConfiguration.Add(fullConfiguration, index);

        return index;
    }

    public bool TrySplitFullConfigurationCached(
        string fullConfiguration,
        [NotNullWhen(true)] out string? buildType,
        [NotNullWhen(true)] out string? platform)
    {
        if (string.IsNullOrEmpty(fullConfiguration))
        {
            buildType = null;
            platform = null;
            return false;
        }

        if (this.splitCfgPlatCache.TryGetValue(fullConfiguration, out (string BuildType, string Platform) cached))
        {
            buildType = cached.BuildType;
            platform = cached.Platform;
            return true;
        }

        if (ModelHelper.TrySplitFullConfiguration(fullConfiguration, out StringSpan buildTypeSpan, out StringSpan platformSpan))
        {
            buildType = BuildTypeNames.ToStringKnown(buildTypeSpan);
            platform = PlatformNames.ToStringKnown(platformSpan);
            this.splitCfgPlatCache.Add(fullConfiguration, (buildType, platform));
            return true;
        }

        buildType = null;
        platform = null;
        return false;
    }

    // Used for Sln file parsing to classify the type of configuration line.
    private enum SetType
    {
        None = 0,
        Active,
        Build,
        Deploy,
    }

    /// <summary>
    /// Applies a .SLN configuration line to the current project configuration.
    /// These have a lot of weird syntax.
    /// </summary>
    /// <remarks>
    /// TODO Move to SlnFileV12 parser.
    /// </remarks>
    public void ParseProjectConfigLine(string name, string value)
    {
        int firstDot = name.IndexOf('.');
        if (firstDot < 0)
        {
            return;
        }

        string projId = name.Substring(0, firstDot);
        if (this.solutionBuilder is null ||
            !this.solutionBuilder.TryGet(projId, out SolutionItemModel? item) ||
            item is not SolutionProjectModel projectModel)
        {
            return;
        }

        SetType setType =
            name.EndsWith(ActiveCfgSuffix) ? SetType.Active :
            name.EndsWith(BuildSuffix) ? SetType.Build :
            name.EndsWith(DeploySuffix) ? SetType.Deploy :
            SetType.None;

        if (setType == SetType.None)
        {
            return;
        }

        int slnCfgEnd = name.Length - setType switch
        {
            SetType.Active => ActiveCfgSuffix.Length,
            SetType.Build => BuildSuffix.Length,
            SetType.Deploy => DeploySuffix.Length,
            _ => throw new InvalidOperationException(),
        };

        firstDot++;
        if (firstDot >= slnCfgEnd)
        {
            return;
        }

        string slnCfg = name.Substring(firstDot, slnCfgEnd - firstDot);
        SolutionConfigIndex slnCfgIndex = this.GetConfigIndex(slnCfg);
        if (!slnCfgIndex.IsValid)
        {
            return;
        }

        if (!this.perProjectCurrent.TryGetValue(projectModel, out SolutionToProjectMappings projectMappings))
        {
            projectMappings = new SolutionToProjectMappings(this, projectModel, out bool _, forceExclude: true);
            this.perProjectCurrent.Add(projectModel, projectMappings);
        }

        ProjectConfigMapping mapping = projectMappings[slnCfgIndex];
        projectMappings[slnCfgIndex] = setType switch
        {
            SetType.Active => mapping with
            {
                BuildType = this.TrySplitFullConfigurationCached(value, out string? projectBuildType, out _) ? projectBuildType : mapping.BuildType,
                Platform = this.TrySplitFullConfigurationCached(value, out _, out string? projectPlatform) ? projectPlatform : mapping.Platform,
            },
            SetType.Build => mapping with { Build = true },
            SetType.Deploy => mapping with { Deploy = true },
            _ => mapping,
        };
    }

    /// <summary>
    /// Used to convert this model to a full list of all solution to project configurations.
    /// This is used to serialize the .SLN file.
    /// </summary>
    public void GetProjectConfigMap(
        SolutionProjectModel projectModel,
        out SolutionToProjectMappings projectMappings,
        out bool supportsConfigs)
    {
        projectMappings = new SolutionToProjectMappings(this, projectModel, out bool isBuildable);
        supportsConfigs = isBuildable || !projectModel.ProjectConfigurationRules.IsNullOrEmpty();

        foreach (ConfigurationRule rule in projectModel.ProjectConfigurationRules.GetStructEnumerable())
        {
            int buildTypeIndex = this.GetBuildTypeIndex(rule.SolutionBuildType);
            int platformIndex = this.GetPlatformIndex(rule.SolutionPlatform);

            if ((!string.IsNullOrEmpty(rule.SolutionBuildType) && buildTypeIndex < 0) ||
                (!string.IsNullOrEmpty(rule.SolutionPlatform) && platformIndex < 0))
            {
                continue;
            }

            this.ApplyRules(in projectMappings, new ScopedRules(buildTypeIndex, platformIndex, [rule]));
        }
    }

    /// <summary>
    /// This just create a mapping of all solution configurations to indexes.
    /// Solution configurations are every combination of buildType and platform.
    /// </summary>
    internal (string SlnKey, SolutionConfigIndex Index)[] CreateMatrixAnnotation()
    {
        (string SlnKey, SolutionConfigIndex Index)[] ret = new (string SlnKey, SolutionConfigIndex Index)[this.matrixSize];
        for (int buildTypeIndex = 0; buildTypeIndex < this.solutionModel.BuildTypes.Count; buildTypeIndex++)
        {
            string buildType = this.solutionModel.BuildTypes[buildTypeIndex];

            for (int platformIndex = 0; platformIndex < this.solutionModel.Platforms.Count; platformIndex++)
            {
                string platform = this.solutionModel.Platforms[platformIndex];

                SolutionConfigIndex idx = new SolutionConfigIndex(this, buildTypeIndex, platformIndex);
                ret[idx.MatrixIndex] = ($"{buildType}|{platform}", idx);
            }
        }

        return ret;
    }

    /// <summary>
    /// Represents all project configurations that are mapped from
    /// all solution configurations for a single project.
    /// </summary>
    internal readonly struct SolutionToProjectMappings
    {
#if DEBUG
        // For debugging, to know which project this is for.
        private readonly SolutionProjectModel projectModel;
#endif
        private readonly ProjectConfigMapping[] mappings;

        public SolutionToProjectMappings(
            SolutionConfigurationMap configMap,
            SolutionProjectModel projectModel,
            out bool isConfigurable,
            bool forceExclude = false)
        {
#if DEBUG
            this.projectModel = projectModel;
#endif
            this.mappings = new ProjectConfigMapping[configMap.matrixSize];

            ConfigurationRuleFollower projectTypeRules = configMap.solutionModel.ProjectTypeTable.GetProjectConfigurationRules(projectModel);
            isConfigurable = projectTypeRules.GetIsBuildable() ?? true;

            for (int iPlatform = 0; iPlatform < configMap.PlatformsCount; iPlatform++)
            {
                string solutionPlatform = configMap.solutionModel.Platforms[iPlatform].Canonical();

                for (int iBuildType = 0; iBuildType < configMap.BuildTypesCount; iBuildType++)
                {
                    string solutionBuildType = configMap.solutionModel.BuildTypes[iBuildType];

                    bool build = projectTypeRules.GetIsBuildable(solutionBuildType, solutionPlatform) ?? true;
                    bool deploy = projectTypeRules.GetIsDeployable(solutionBuildType, solutionPlatform) ?? false;
                    string projectBuildType = projectTypeRules.GetProjectBuildType(solutionBuildType, solutionPlatform) ?? solutionBuildType;
                    string projectPlatform = projectTypeRules.GetProjectPlatform(solutionBuildType, solutionPlatform) ?? solutionPlatform;

                    this[configMap.ToIndex(iBuildType, iPlatform)] =
                        new ProjectConfigMapping(projectBuildType, projectPlatform, !forceExclude && build, !forceExclude && deploy);
                }
            }
        }

        public ProjectConfigMapping this[SolutionConfigIndex index]
        {
            get => this.mappings[index.MatrixIndex];
            set => this.mappings[index.MatrixIndex] = value;
        }

#if DEBUG
        public override string ToString()
        {
            return this.projectModel.DisplayName ?? string.Empty;
        }
#endif
    }

    /// <summary>
    /// Represents an index into the matrix of solution to project configurations mappings.
    /// </summary>
    internal readonly struct SolutionConfigIndex
    {
        private readonly int index;

        public SolutionConfigIndex() => this.index = -1;

        public SolutionConfigIndex(SolutionConfigurationMap map, string buildType, string platform)
            : this(map, map.GetBuildTypeIndex(buildType), map.GetPlatformIndex(platform))
        {
        }

        public SolutionConfigIndex(SolutionConfigurationMap map, int buildType, int platForm)
        {
            bool unknown = buildType < 0 || buildType >= map.BuildTypesCount || platForm < 0 || platForm >= map.PlatformsCount;
            this.index = unknown ? -1 : (buildType * map.PlatformsCount) + platForm;
        }

        public bool IsValid => this.index >= 0;

        public int MatrixIndex => this.index;

        public string BuildType(SolutionConfigurationMap map) => map.BuildTypeFromIndex(this);

        public string Platform(SolutionConfigurationMap map) => map.PlatformFromIndex(this);
    }
}
