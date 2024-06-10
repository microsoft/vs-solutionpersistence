// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Linq;
using Microsoft.VisualStudio.SolutionPersistence.Model;
using Microsoft.VisualStudio.SolutionPersistence.Utilities;
using static Microsoft.VisualStudio.SolutionPersistence.Model.SolutionModel;

namespace Microsoft.VisualStudio.SolutionPersistence.Serializer.SlnV12;

/// <summary>
/// Extension methods for model to make it easier to get SlnV12 properties from model.
/// </summary>
public static class SlnV12Extensions
{
    /// <summary>
    /// Gets extra info for VS to open the solution with a specific installed version.
    /// (e.g. # Visual Studio Version 17 in SLN file).
    /// </summary>
    /// <param name="model">The solution model.</param>
    /// <returns>The version of Visual Studio to open the solution with.</returns>
    public static string? GetOpenWithVisualStudio(this SolutionModel model)
    {
        Argument.ThrowIfNull(model, nameof(model));
        SolutionPropertyBag? properties = model.Properties.FindByItemRef(SectionName.VisualStudio);
        return properties is null ? null : properties.TryGetValue(SlnConstants.OpenWith, out string? openWith) ? openWith : null;
    }

    /// <summary>
    /// Sets extra info for VS to open the solution with a specific installed version.
    /// (e.g. # Visual Studio Version 17 in SLN file).
    /// </summary>
    /// <param name="modelBuider">The solution model builder.</param>
    /// <param name="openWith">The version of Visual Studio to open the solution with.</param>
    public static void SetOpenWithVisualStudio(this SolutionModel.Builder modelBuider, string openWith)
    {
        Argument.ThrowIfNull(modelBuider, nameof(modelBuider));
        modelBuider.EnsureProperties(SectionName.VisualStudio)
            .Add(SlnConstants.OpenWith, openWith);
    }

    /// <summary>
    /// Gets the solution file property that is used to determine if the solution should be shown
    /// in Visual Studio's solution explorer. This property is obsolete and should not be used.
    /// </summary>
    /// <param name="model">The solution model.</param>
    /// <returns>The value of the option or null if it is not explicitly set.</returns>
    public static bool? GetHideSolutionNode(this SolutionModel model)
    {
        Argument.ThrowIfNull(model, nameof(model));
        SolutionPropertyBag? properties = model.Properties.FindByItemRef(SectionName.SolutionProperties);
        return
            properties is null ? null :
            !properties.TryGetValue(SlnConstants.HideSolutionNode, out string? hideSolutionNodeStr) ? null :
            !bool.TryParse(hideSolutionNodeStr, out bool hideSolutionNode) ? null :
            hideSolutionNode;
    }

    /// <summary>
    /// Sets the solution file property that is used to determine if the solution should be shown
    /// in Visual Studio's solution explorer. This property is obsolete and should not be used.
    /// </summary>
    /// <param name="modelBuilder">The solution model builder.</param>
    /// <param name="hideSolutionNode">The value of the options.</param>
    public static void SetHideSolutionNode(this SolutionModel.Builder modelBuilder, bool hideSolutionNode)
    {
        Argument.ThrowIfNull(modelBuilder, nameof(modelBuilder));
        if (hideSolutionNode)
        {
            modelBuilder.EnsureProperties(SectionName.SolutionProperties)
                .Add(SlnConstants.HideSolutionNode, hideSolutionNode.ToString(CultureInfo.InvariantCulture));
        }
        else
        {
            SolutionPropertyBag? solutionProperties = modelBuilder.TryGetProperties(SectionName.SolutionProperties);
            if (solutionProperties is not null)
            {
                solutionProperties.Remove(SlnConstants.HideSolutionNode);
                if (solutionProperties.Count == 0)
                {
                    _ = modelBuilder.RemoveProperties(SectionName.SolutionProperties);
                }
            }
        }
    }

    /// <summary>
    /// Automatically converts special SlnV12 property bags into their equivalent model concepts.
    /// This handles property tables that used to represent configurations, solution folder, files and dependencies.
    /// If the properties are not special types, they will be added as regular property bags.
    /// </summary>
    /// <param name="itemBuilder">A model builder.</param>
    /// <param name="properties">The properties to add to the model.</param>
    public static void AddSlnProperties(this SolutionItemModel.Builder itemBuilder, SolutionPropertyBag? properties)
    {
        Argument.ThrowIfNull(itemBuilder, nameof(itemBuilder));
        if (properties is null)
        {
            return;
        }

        switch (SectionName.InternKnownSectionName(properties.Id))
        {
            case SectionName.SolutionItems when itemBuilder is SolutionFolderModel.Builder folderBuilder:
                foreach (string fileName in properties.PropertyNames)
                {
                    folderBuilder.AddFile(PathExtensions.ConvertFromPersistencePath(fileName));
                }

                break;
            case SectionName.ProjectDependencies when itemBuilder is SolutionProjectModel.Builder projectBuilder:
                foreach (string dependencyProjectId in properties.PropertyNames)
                {
                    projectBuilder.AddDependency(dependencyProjectId);
                }

                break;
            default:
                _ = itemBuilder.AddProperties(properties);
                break;
        }
    }

    public static IEnumerable<SolutionPropertyBag> GetSlnProperties(this SolutionItemModel model, SolutionModel solutionModel)
    {
        Argument.ThrowIfNull(model, nameof(model));
        Argument.ThrowIfNull(solutionModel, nameof(solutionModel));

        ListBuilderStruct<SolutionPropertyBag> slnProperties = new ListBuilderStruct<SolutionPropertyBag>((model.Properties?.Count ?? 0) + 1);

        IReadOnlyList<string>? dependencies = (model as SolutionProjectModel)?.Dependencies;
        if (!dependencies.IsNullOrEmpty())
        {
            SolutionPropertyBag propertyBag = new SolutionPropertyBag(SectionName.ProjectDependencies, PropertiesScope.PostLoad, dependencies.Count);
            foreach (string dependency in dependencies)
            {
                SolutionProjectModel? parentProjectModel = solutionModel.SolutionProjects.FindByItemRef(dependency);
                if (parentProjectModel is null || parentProjectModel.Id == Guid.Empty)
                {
                    throw new ArgumentException(null, nameof(solutionModel));
                }

                string dependencyProjectId = parentProjectModel.Id.ToSlnString();
                propertyBag.Add(dependencyProjectId, dependencyProjectId);
            }

            slnProperties.Add(propertyBag);
        }

        IReadOnlyList<string>? files = (model as SolutionFolderModel)?.Files;
        if (!files.IsNullOrEmpty())
        {
            SolutionPropertyBag propertyBag = new SolutionPropertyBag(SectionName.SolutionItems, PropertiesScope.PreLoad, files.Count);
            foreach (string file in files)
            {
                string persistenceFile = PathExtensions.ConvertToPersistencePath(file);
                propertyBag.Add(persistenceFile, persistenceFile);
            }

            slnProperties.Add(propertyBag);
        }

        foreach (SolutionPropertyBag propertyBag in model.Properties.GetStructEnumerable())
        {
            if (SectionName.InternKnownSectionName(propertyBag.Id) is
                not SectionName.ProjectDependencies and
                not SectionName.SolutionItems)
            {
                slnProperties.Add(propertyBag);
            }
        }

        return slnProperties.ToArray();
    }

    /// <summary>
    /// Automatically converts special SlnV12 property bags into their equivalent model concepts.
    /// This handles property tables that used to represent configurations, solution folder, files and dependencies.
    /// If the properties are not special types, they will be added as regular property bags.
    /// </summary>
    /// <param name="solutionBuilder">A model builder.</param>
    /// <param name="properties">The properties to add to the model.</param>
    public static void AddSlnProperties(this Builder solutionBuilder, SolutionPropertyBag? properties)
    {
        Argument.ThrowIfNull(solutionBuilder, nameof(solutionBuilder));
        if (properties is null)
        {
            return;
        }

        switch (SectionName.InternKnownSectionName(properties.Id))
        {
            case SectionName.SolutionConfigurationPlatforms:
                foreach (string slnConfiguration in properties.PropertyNames)
                {
                    // For some reason the description was stored in this property table.
                    if (StringComparer.OrdinalIgnoreCase.Equals(slnConfiguration, SlnConstants.Description))
                    {
                        solutionBuilder.Description = properties[slnConfiguration];
                        continue;
                    }

                    solutionBuilder.AddSolutionConfiguration(slnConfiguration);
                }

                break;

            case SectionName.ProjectConfigurationPlatforms:
                solutionBuilder.SetProjectConfigurationPlatforms(properties);
                break;

            case SectionName.NestedProjects:
                foreach ((string childProjectIdStr, string parentProjectIdStr) in properties)
                {
                    if (Guid.TryParse(childProjectIdStr, out Guid childProjectId))
                    {
                        SolutionItemModel.Builder? childBuilder = solutionBuilder.Items.FirstOrDefault(x => x.ItemId == childProjectId);
                        if (childBuilder is not null)
                        {
                            childBuilder.Parent = parentProjectIdStr;
                        }
                    }
                }

                break;
            case SectionName.SolutionProperties:
                if (properties.TryGetValue(SlnConstants.HideSolutionNode, out string? hideSolutionNodeStr) &&
                    bool.TryParse(hideSolutionNodeStr, out bool hideSolutionNode))
                {
                    if (hideSolutionNode)
                    {
                        solutionBuilder.SetHideSolutionNode(true);
                    }

                    if (properties.Count == 1)
                    {
                        return;
                    }
                    else
                    {
                        properties.Remove(SlnConstants.HideSolutionNode);
                        _ = solutionBuilder.AddProperties(properties);
                    }

                    break;
                }

                break;
            case SectionName.ExtensibilityGlobals:
                if (properties.TryGetValue(SlnConstants.SolutionGuid, out string? solutionGuidStr) &&
                    Guid.TryParse(solutionGuidStr, out Guid solutionId))
                {
                    if (solutionId != Guid.Empty)
                    {
                        solutionBuilder.SolutionId = solutionId;
                    }

                    if (properties.Count == 1)
                    {
                        return;
                    }
                    else
                    {
                        properties.Remove(SlnConstants.SolutionGuid);
                        _ = solutionBuilder.AddProperties(properties);
                    }
                }

                break;
            default:
                _ = solutionBuilder.AddProperties(properties);
                break;
        }
    }

    public static IEnumerable<SolutionPropertyBag> GetSlnProperties(this SolutionModel model)
    {
        Argument.ThrowIfNull(model, nameof(model));
        List<SolutionPropertyBag> slnProperties = new List<SolutionPropertyBag>((model.Properties?.Count ?? 0) + 1);

        AddIfNotNull(slnProperties, GetSolutionConfigurationPlatforms(model));
        AddIfNotNull(slnProperties, GetProjectConfigurationPlatforms(model));
        AddIfNotNull(slnProperties, GetSolutionProperties(model));
        AddIfNotNull(slnProperties, GetNestedProjects(model));
        AddIfNotNull(slnProperties, GetExtensibilityGlobals(model));

        foreach (SolutionPropertyBag propertyBag in model.Properties.GetStructEnumerable())
        {
            if (SectionName.InternKnownSectionName(propertyBag.Id) is
                not SectionName.SolutionConfigurationPlatforms and
                not SectionName.ProjectConfigurationPlatforms and
                not SectionName.SolutionProperties and
                not SectionName.NestedProjects and
                not SectionName.ExtensibilityGlobals and
                not SectionName.VisualStudio)
            {
                slnProperties.Add(propertyBag);
            }
        }

        return slnProperties;

        static void AddIfNotNull<T>(List<T> list, T? item)
        {
            if (item is not null)
            {
                list.Add(item);
            }
        }

        // All solution configurations
        static SolutionPropertyBag? GetSolutionConfigurationPlatforms(SolutionModel model)
        {
            if (model.Platforms.Count == 0 && model.BuildTypes.Count == 0)
            {
                return null;
            }

            int size = model.Platforms.Count * model.BuildTypes.Count;
            if (!model.Description.IsNullOrEmpty())
            {
                size++;
            }

            SolutionPropertyBag propertyBag = new SolutionPropertyBag(
                SectionName.SolutionConfigurationPlatforms,
                PropertiesScope.PreLoad,
                capacity: size);

            foreach (string buildType in model.BuildTypes)
            {
                foreach (string platform in model.Platforms)
                {
                    string slnConfiguration = $"{buildType}|{platform}";
                    propertyBag.Add(slnConfiguration, slnConfiguration);
                }
            }

            if (!model.Description.IsNullOrEmpty())
            {
                propertyBag.Add(SlnConstants.Description, model.Description);
            }

            return propertyBag;
        }

        // All solution to project configuration mappings and build mappings
        static SolutionPropertyBag? GetProjectConfigurationPlatforms(SolutionModel model)
        {
            if (model.Platforms.Count == 0 && model.BuildTypes.Count == 0)
            {
                return null;
            }

            SolutionConfigurationMap cfgMap = new SolutionConfigurationMap(model);
            (string SlnKey, SolutionConfigurationMap.SolutionConfigIndex Index)[] indexer = cfgMap.CreateMatrixAnnotation();

            int size = indexer.Length * model.SolutionProjects.Count * 3;
            SolutionPropertyBag propertyBag = new SolutionPropertyBag(SectionName.ProjectConfigurationPlatforms, PropertiesScope.PostLoad, size);

            foreach (SolutionProjectModel projectModel in model.SolutionProjects)
            {
                // Gets the mapping of solution to project configurations
                cfgMap.GetProjectConfigMap(projectModel, out SolutionConfigurationMap.SolutionToProjectMappings prjSlnCfgInfo, out bool writeConfigurations);
                if (!writeConfigurations)
                {
                    continue;
                }

                string projectId = projectModel.Id.ToSlnString();

                for (int i = 0; i < indexer.Length; i++)
                {
                    ref (string SlnKey, SolutionConfigurationMap.SolutionConfigIndex Index) entry = ref indexer[i];
                    ProjectConfigMapping mapping = prjSlnCfgInfo[entry.Index];
                    if (!mapping.IsValidBuildType || !mapping.IsValidPlatform)
                    {
                        continue;
                    }

                    // Default project mapping in SLN was to use "Any CPU"
                    string platform = mapping.Platform;
                    if (platform == PlatformNames.AnyCPU)
                    {
                        platform = PlatformNames.AnySpaceCPU;
                    }

                    string prjCfgPlatString = $"{mapping.BuildType}|{platform}";

                    WriteProperty(propertyBag, projectId, entry.SlnKey, SolutionConfigurationMap.ActiveCfgSuffix, prjCfgPlatString);
                    if (mapping.Build)
                    {
                        WriteProperty(propertyBag, projectId, entry.SlnKey, SolutionConfigurationMap.BuildSuffix, prjCfgPlatString);
                    }

                    if (mapping.Deploy)
                    {
                        WriteProperty(propertyBag, projectId, entry.SlnKey, SolutionConfigurationMap.DeploySuffix, prjCfgPlatString);
                    }
                }
            }

            return propertyBag;

            static void WriteProperty(SolutionPropertyBag propertyBag, string projectId, string slnCfg, string name, string value) =>
                propertyBag.Add(projectId + '.' + slnCfg + name, value);
        }

        // HideSolutionNode property
        static SolutionPropertyBag GetSolutionProperties(SolutionModel model)
        {
            SolutionPropertyBag? additionalProperties = ModelHelper.FindByItemRef(model.Properties, SectionName.SolutionProperties);
            SolutionPropertyBag propertyBag = new SolutionPropertyBag(SectionName.SolutionProperties, PropertiesScope.PreLoad, 1 + additionalProperties?.Count ?? 0)
            {
                { SlnConstants.HideSolutionNode, model.GetHideSolutionNode().GetValueOrDefault(false) ? "TRUE" : "FALSE" },
            };

            if (additionalProperties is not null)
            {
                foreach ((string propertyName, string value) in additionalProperties)
                {
                    // Ignore OpenWithVS as there is special logic to handle that case.
                    if (!StringComparer.Ordinal.Equals(propertyName, SlnConstants.OpenWith))
                    {
                        propertyBag.Add(propertyName, value);
                    }
                }
            }

            return propertyBag;
        }

        // Project parents to nest projects under solution folders.
        static SolutionPropertyBag? GetNestedProjects(SolutionModel model)
        {
            if (!AnyNestedProjects(model))
            {
                return null;
            }

            int count = model.SolutionItems.Count(static x => x.Parent is not null);

            SolutionPropertyBag propertyBag = new SolutionPropertyBag(SectionName.NestedProjects, PropertiesScope.PreLoad, count);
            foreach (SolutionItemModel item in model.SolutionItems)
            {
                if (item.Parent is not null)
                {
                    propertyBag.Add(item.Id.ToSlnString(), item.Parent.Id.ToSlnString());
                }
            }

            return propertyBag;

            static bool AnyNestedProjects(SolutionModel model) =>
                model.SolutionItems.Any(static item => item.Parent is not null);
        }

        static SolutionPropertyBag? GetExtensibilityGlobals(SolutionModel model)
        {
            SolutionPropertyBag? additionalProperties = ModelHelper.FindByItemRef(model.Properties, SectionName.ExtensibilityGlobals);

            if (model.SolutionId is null)
            {
                return additionalProperties;
            }

            SolutionPropertyBag propertyBag = new SolutionPropertyBag(SectionName.ExtensibilityGlobals, PropertiesScope.PostLoad, 1 + additionalProperties?.Count ?? 0)
            {
                { SlnConstants.SolutionGuid, (model.SolutionId ?? Guid.NewGuid()).ToSlnString() },
            };

            if (additionalProperties is not null)
            {
                propertyBag.AddRange(additionalProperties);
            }

            return propertyBag;
        }
    }
}
