// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;
using Microsoft.VisualStudio.SolutionPersistence.Utilities;

namespace Microsoft.VisualStudio.SolutionPersistence.Model;

/// <summary>
/// Wrapper to query list of project types.
/// </summary>
internal sealed partial class ProjectTypeTable
{
    private readonly bool isBuiltIn;
    private readonly Dictionary<string, ProjectType> fromExtension;
    private readonly Dictionary<string, ProjectType> fromName;
    private readonly Dictionary<Guid, ProjectType> fromProjectTypeId;
    private readonly IReadOnlyList<ConfigurationRule> defaultRules;
    private readonly List<ProjectType> projectTypesList;

    internal ProjectTypeTable()
        : this([], logger: null)
    {
    }

    internal ProjectTypeTable(List<ProjectType> projectTypes, ISerializerLogger? logger)
    : this(isBuiltIn: false, projectTypes, logger)
    {
    }

    private ProjectTypeTable(bool isBuiltIn, List<ProjectType> projectTypes, ISerializerLogger? logger)
    {
        this.isBuiltIn = isBuiltIn;
        this.projectTypesList = projectTypes;
        this.fromExtension = new(this.ProjectTypes.Count, StringComparer.OrdinalIgnoreCase);
        this.fromName = new(this.ProjectTypes.Count, StringComparer.OrdinalIgnoreCase);
        this.fromProjectTypeId = new(this.ProjectTypes.Count);

        foreach (ProjectType type in projectTypes.GetStructEnumerable())
        {
            if (!type.Extension.IsNullOrEmpty() &&
                !this.fromExtension.TryAdd(GetExtension(type.Extension), type))
            {
                logger?.LogError($"Duplicate extension '{type.Extension}' for project type '{type.GetDisplayName()}'.");
                _ = this.projectTypesList.Remove(type);
            }

            if (!type.Name.IsNullOrEmpty())
            {
                if (!this.fromName.TryAdd(type.Name, type))
                {
                    logger?.LogError($"Duplicate name '{type.Name}' for project type '{type.GetDisplayName()}'.");
                    _ = this.projectTypesList.Remove(type);
                }

                // If a name isn't provided, it is just to map an extension to a project type.
                if (type.ProjectTypeId != Guid.Empty && !this.fromProjectTypeId.TryAdd(type.ProjectTypeId, type))
                {
                    logger?.LogError($"Duplicate project type id '{type.ProjectTypeId}' for project type '{type.GetDisplayName()}'.");
                    _ = this.projectTypesList.Remove(type);
                }
            }

            if (string.IsNullOrEmpty(type.Name) && string.IsNullOrEmpty(type.Extension) && type.ProjectTypeId == Guid.Empty)
            {
                if (this.defaultRules is not null)
                {
                    logger?.LogError("Multiple default project types defined.");
                    _ = this.projectTypesList.Remove(type);
                }

                this.defaultRules ??= type.ConfigurationRules;
            }
        }

        foreach (ProjectType type in projectTypes.GetStructEnumerable())
        {
            if (!type.BasedOn.IsNullOrEmpty())
            {
                if (this.GetBasedOnType(type) is null)
                {
                    logger?.LogError($"BasedOn '{type.BasedOn}' not found for project type '{type.GetDisplayName()}'.");
                    _ = this.projectTypesList.Remove(type);
                }

                // Check for loops in the BasedOn chain using Floyd's cycle-finding algorithm.
                ProjectType? currentSlow = type;
                ProjectType? currentFast = this.GetBasedOnType(type);
                while (currentSlow is not null && currentFast is not null)
                {
                    if (object.ReferenceEquals(currentSlow, currentFast))
                    {
                        logger?.LogError($"BasedOn loop detected for project type '{type.GetDisplayName()}'.");
                        _ = this.projectTypesList.Remove(type);
                        break;
                    }

                    currentSlow = this.GetBasedOnType(currentSlow);
                    currentFast = this.GetBasedOnType(this.GetBasedOnType(currentFast));
                }
            }
        }

        this.defaultRules ??= [];

        static string GetExtension(string extension) => extension.StartsWith('.') ? extension : $".{extension}";
    }

    internal IReadOnlyList<ProjectType> ProjectTypes => this.projectTypesList;

    internal Guid GetProjectTypeId(string? alias, StringSpan extension)
    {
        ProjectType? type = this.GetForName(alias) ?? this.GetForExtension(extension.ToString());
        return
            type is not null ? this.GetProjectTypeId(type) :
            !this.isBuiltIn ? BuiltInTypes.GetProjectTypeId(alias, extension) :
            Guid.Empty;
    }

    private Guid GetProjectTypeId(ProjectType? type)
    {
        // If the type doesn't have a project type id, keep searching on the BasedOn type.
        while (type is not null && type.ProjectTypeId == Guid.Empty)
        {
            type = this.GetBasedOnType(type);
        }

        return type is null ? Guid.Empty : type.ProjectTypeId;
    }

    // Figures out what the most concise friendly type name of the project type is, if it fails use the project type id.
    internal string? GetConciseType(SolutionProjectModel projectModel)
    {
        // Get TypeId to add to the Project element.
        return
            !this.TryGetProjectType(projectModel, out ProjectType? projectType, out bool impliedFromExtension) ? GetTypeFromModel(projectModel) :
            !impliedFromExtension ? GetTypeFromProjectType(projectType) :
            null;

        // TODO! See if Guid->FriendlyName mapping is needed.
        string? GetTypeFromProjectType(ProjectType projectType) =>
            projectType.Name.NullIfEmpty() ?? this.GetProjectTypeId(projectType).ToString();

        static string? GetTypeFromModel(SolutionProjectModel modelProject) =>
            modelProject.TypeId == Guid.Empty ? modelProject.TypeRef : modelProject.TypeId.ToString();
    }

    // Gets all of the configuration rules that apply to the project.
    internal ConfigurationRuleFollower GetProjectConfigurationRules(SolutionProjectModel projectModel)
    {
        // Rules are ordered most general to most specific.
        if (this.TryGetProjectType(projectModel, out ProjectType? type, out _))
        {
            List<ConfigurationRule> rules = new List<ConfigurationRule>(32);

            // Get the default built-in rules.
            if (!this.isBuiltIn)
            {
                rules.AddRange(BuiltInTypes.defaultRules);
            }

            // Get all the rules and based on rules for the project type.
            GetProjectTypeConfigurationRules(type, rules);

            // Get the default rules in the solution. These intentionally are higher priority than type rules.
            rules.AddRange(this.defaultRules);
            return new ConfigurationRuleFollower(rules);
        }
        else
        {
            return new ConfigurationRuleFollower([]);
        }

        void GetProjectTypeConfigurationRules(ProjectType? type, List<ConfigurationRule> rules)
        {
            if (type is null)
            {
                return;
            }

            GetProjectTypeConfigurationRules(this.GetBasedOnType(type), rules);
            rules.AddRange(type.ConfigurationRules);
        }
    }

    private ProjectType? GetBasedOnType(ProjectType? type)
    {
        return type is not null && !type.BasedOn.IsNullOrEmpty() &&
            (this.TryGetProjectType(Guid.Empty, type.BasedOn, null, out ProjectType? basedOnType, out _) ||
            BuiltInTypes.TryGetProjectType(Guid.Empty, null, type.BasedOn, out basedOnType, out _)) ?
            basedOnType :
            null;
    }

    private bool TryGetProjectType(
        SolutionProjectModel projectModel,
        [NotNullWhen(true)] out ProjectType? type,
        out bool impliedFromExtension)
    {
        return this.TryGetProjectType(
            projectModel.TypeId,
            projectModel.TypeRef,
            projectModel.Extension,
            out type,
            out impliedFromExtension);
    }

    private bool TryGetProjectType(
        [Optional] Guid projectTypeId,
        string? typeRef,
        string? extension,
        [NotNullWhen(true)] out ProjectType? type,
        out bool impliedFromExtension)
    {
        // If the TypeRef is a Guid, use it as the projectTypeId instead.
        if (Guid.TryParse(typeRef, out Guid typeId))
        {
            typeRef = null;
            projectTypeId = typeId;
        }

        // Only pick the implied type from the extension if it matches the typeRef.
        type = this.GetForExtension(extension);
        if (type is not null)
        {
            Guid typeProjectTypeId = this.GetProjectTypeId(type);
            if ((projectTypeId == Guid.Empty || typeProjectTypeId == projectTypeId) &&
                (typeRef.IsNullOrEmpty() || StringComparer.OrdinalIgnoreCase.Equals(typeRef, type.Name)))
            {
                impliedFromExtension = true;
                return true;
            }
        }

        type = this.GetForName(typeRef);
        if (type is not null)
        {
            impliedFromExtension = false;
            return true;
        }

        if (this.fromProjectTypeId.TryGetValue(projectTypeId, out type))
        {
            impliedFromExtension = false;
            return true;
        }

        // If not found in solution scope, try implicit types.
        if (!this.isBuiltIn)
        {
            if (BuiltInTypes.TryGetProjectType(projectTypeId, typeRef, extension, out type, out impliedFromExtension))
            {
                return true;
            }

            // TODO: Search for type in external sources and add to this table if found.
        }

        type = null;
        impliedFromExtension = false;
        return false;
    }

    private ProjectType? GetForExtension(string? extension)
    {
        return !extension.IsNullOrEmpty() && this.fromExtension.TryGetValue(extension, out ProjectType? type) ? type : null;
    }

    private ProjectType? GetForName(string? name)
    {
        return !name.IsNullOrEmpty() && this.fromName.TryGetValue(name, out ProjectType? type) ? type : null;
    }
}
