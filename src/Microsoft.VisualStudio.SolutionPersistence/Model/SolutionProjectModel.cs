// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.VisualStudio.SolutionPersistence.Utilities;

namespace Microsoft.VisualStudio.SolutionPersistence.Model;

/// <summary>
/// Represents a project in the solution model.
/// </summary>
public sealed partial class SolutionProjectModel : SolutionItemModel
{
    private Guid typeId;

    [SetsRequiredMembers]
    public SolutionProjectModel(string filePath, Guid typeId, string typeRef)
    {
        this.typeId = typeId;
        this.TypeRef = typeRef;
        this.FilePath = filePath;
        this.Extension = PathExtensions.GetExtension(this.FilePath).ToString();
    }

    public override Guid TypeId => this.typeId;

    internal void SetTypeId(Guid id)
    {
        this.typeId = id;
    }

    public string FilePath { get; }

    public override string ItemRef => this.FilePath;

    public string TypeRef { get; }

    public string Extension { get; }

    public string? DisplayName { get; init; }

    public override string CanonicalDisplayName => this.DisplayName ?? this.FilePath.GetStandardDisplayName().ToString();

    /// <summary>
    /// Gets or sets list of ItemRef's for the dependencies of this project.
    /// </summary>
    public IReadOnlyList<string>? Dependencies { get; set; }

    // NOTE: This has a setters.
    public IReadOnlyList<ConfigurationRule>? ProjectConfigurationRules { get; set; }

    internal void SetProjectConfigurationRules(ConfigurationRule[]? rules) => this.ProjectConfigurationRules = rules.IsNullOrEmpty() ? null : rules;
}
