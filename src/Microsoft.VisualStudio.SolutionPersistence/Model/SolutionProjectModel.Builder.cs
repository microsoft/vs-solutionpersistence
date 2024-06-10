// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq;
using Microsoft.VisualStudio.SolutionPersistence.Utilities;

namespace Microsoft.VisualStudio.SolutionPersistence.Model;

/// <summary>
/// Represents a project in the solution model.
/// </summary>
public sealed partial class SolutionProjectModel
{
    public new sealed class Builder : SolutionItemModel.Builder
    {
        private List<string>? dependencies;
        private List<ConfigurationRule>? projectConfigurationRules;

        internal Builder(SolutionModel.Builder solutionBuilder, string filePath)
            : base(solutionBuilder)
        {
            this.FilePath = filePath;
        }

        internal Builder(SolutionModel.Builder solutionBuilder, SolutionProjectModel projectModel)
            : base(solutionBuilder, projectModel)
        {
            this.FilePath = projectModel.FilePath;
            this.ProjectType = projectModel.TypeRef.NullIfEmpty() ?? (projectModel.TypeId == Guid.Empty ? null : projectModel.TypeId.ToString());
            this.Parent = projectModel.Parent?.Id.ToString();
            this.DisplayName = projectModel.DisplayName;
            this.projectConfigurationRules = projectModel.ProjectConfigurationRules?.ToList();
            this.dependencies = projectModel.Dependencies?.ToList();
        }

        public override string? ItemRef => this.FilePath;

        public string FilePath { get; private set; }

        public override bool IsValid => !string.IsNullOrEmpty(this.FilePath);

        public string? ProjectType { get; set; }

        public string? DisplayName { get; set; }

        // Dependency can be ProjectId guid or ItemRef to a project.
        public void AddDependency(string dep)
        {
            this.dependencies ??= [];
            this.dependencies.Add(dep);
        }

        public void AddConfigurationRule(ConfigurationRule configurationRule)
        {
            this.projectConfigurationRules ??= [];
            this.projectConfigurationRules.Add(configurationRule);
        }

        private protected override void ResolveParentAndTypeInternal(SolutionItemModel item)
        {
            if (item is not SolutionProjectModel prj)
            {
                return;
            }

            if (prj.TypeId == Guid.Empty)
            {
                Guid typeId = this.SolutionBuilder.ProjectTypeTable.GetProjectTypeId(
                    alias: this.ProjectType,
                    extension: PathExtensions.GetExtension(this.FilePath));
                prj.SetTypeId(typeId);
            }
        }

        // Dependencies need to be resolved after the type id guids are resolved, otherwise they may not
        // be available to add into the model.
        internal override void ResolveDependencies(SolutionItemModel item)
        {
            base.ResolveDependencies(item);

            if (item is not SolutionProjectModel prj)
            {
                return;
            }

            if (this.dependencies is not null)
            {
                List<string> deps = new(this.dependencies.Count);
                foreach (string d in this.dependencies)
                {
                    if (this.SolutionBuilder.Linker.TryGet(d, out SolutionItemModel? depItem) && depItem is SolutionProjectModel depPrj)
                    {
                        if (depPrj.Id == Guid.Empty)
                        {
                            throw new InvalidOperationException();
                        }

                        deps.Add(depPrj.FilePath);
                    }
                }

                prj.Dependencies = deps;
            }

            this.dependencies = null;
        }

        private protected override Guid CreateId(SolutionItemModel item)
        {
            return this.SolutionBuilder.DefaultIdGenerator.CreateIdFrom(this.FilePath);
        }

        private protected override SolutionItemModel ToModelInternal(out bool needsLinking)
        {
            needsLinking = this.dependencies is not null;

            if (!Guid.TryParse(this.ProjectType, out Guid projectTypeId))
            {
                projectTypeId = Guid.Empty;
                needsLinking = true;
            }

            List<ConfigurationRule>? projectConfigurationRules = this.projectConfigurationRules;
            this.projectConfigurationRules = null;

            return new SolutionProjectModel(this.FilePath, projectTypeId, this.ProjectType ?? string.Empty)
            {
                DisplayName = this.DisplayName,
                ProjectConfigurationRules = projectConfigurationRules,
            };
        }
    }
}
