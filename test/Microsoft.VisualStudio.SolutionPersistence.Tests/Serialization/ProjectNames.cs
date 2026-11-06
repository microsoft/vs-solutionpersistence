// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Serialization;

/// <summary>
/// Tests explicit names for projects that share a conventional manifest file name.
/// </summary>
public sealed class ProjectNames
{
    private const string ProjectType = "0D916ADF-294A-4D07-A543-8F58229C3415";

    /// <summary>
    /// Explicit names participate in sibling validation before projects are added.
    /// </summary>
    /// <param name="inFolder">Whether projects share a solution folder rather than the root.</param>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AddNamedProjects(bool inFolder)
    {
        SolutionModel solution = new SolutionModel();
        SolutionFolderModel? folder = inFolder ? solution.AddFolder("/Applications/") : null;
        SolutionProjectModel first = solution.AddProject(Path.Join("app1", "Cargo.toml"), ProjectType, folder, "app1");
        SolutionProjectModel second = solution.AddProject(Path.Join("app2", "Cargo.toml"), ProjectType, folder, "app2");

        Assert.Equal("app1", first.ActualDisplayName);
        Assert.Equal("app2", second.ActualDisplayName);
        SolutionArgumentException exception = Assert.Throws<SolutionArgumentException>(() => solution.AddProject(Path.Join("app3", "Cargo.toml"), ProjectType, folder, "APP1"));
        Assert.Equal(SolutionErrorType.DuplicateProjectName, exception.Type);
        _ = Assert.Throws<SolutionArgumentException>(() => solution.AddProject(first.FilePath, ProjectType, folder, "other"));
        Assert.Equal(2, solution.SolutionProjects.Count);

        SolutionModel copy = new SolutionModel(solution);
        Assert.Equal("app1", copy.SolutionProjects[0].DisplayNameOverride);
        Assert.Equal("app2", copy.SolutionProjects[1].DisplayNameOverride);
        Assert.Equal("app1", copy.SolutionProjects[0].ActualDisplayName);
        Assert.Equal("app2", copy.SolutionProjects[1].ActualDisplayName);
    }

    /// <summary>
    /// Failed name changes leave both the project and the model valid.
    /// </summary>
    [Fact]
    public void NameChangesAreValidated()
    {
        SolutionModel solution = new SolutionModel();
        SolutionProjectModel first = solution.AddProject(Path.Join("app1", "Cargo.toml"), ProjectType, null, "app1");
        SolutionProjectModel second = solution.AddProject(Path.Join("app2", "Cargo.toml"), ProjectType, null, "app2");
        _ = Assert.Throws<SolutionArgumentException>(() => second.DisplayNameOverride = "APP1");
        Assert.Equal("app2", second.DisplayNameOverride);
        _ = Assert.ThrowsAny<ArgumentException>(() => second.DisplayNameOverride = string.Empty);
        Assert.Equal("app2", second.DisplayNameOverride);

        first.DisplayNameOverride = null;
        Assert.Equal("Cargo", first.ActualDisplayName);
        _ = Assert.Throws<SolutionArgumentException>(() => second.DisplayNameOverride = null);
        Assert.Equal("app2", second.ActualDisplayName);

        second.FilePath = Path.Join("app2", "Renamed.toml");
        Assert.Equal("app2", second.ActualDisplayName);
        second.DisplayNameOverride = null;
        Assert.Equal("Renamed", second.ActualDisplayName);
    }

    /// <summary>
    /// Explicit names are scoped to their containing solution folder.
    /// </summary>
    [Fact]
    public void NamesInDifferentFolders()
    {
        SolutionModel solution = new SolutionModel();
        SolutionFolderModel firstFolder = solution.AddFolder("/First/");
        SolutionFolderModel secondFolder = solution.AddFolder("/Second/");
        _ = solution.AddProject(Path.Join("app1", "Cargo.toml"), ProjectType, firstFolder, "app");
        SolutionProjectModel second = solution.AddProject(Path.Join("app2", "Cargo.toml"), ProjectType, secondFolder, "app");

        _ = Assert.Throws<SolutionArgumentException>(() => second.MoveToFolder(firstFolder));
        Assert.Same(secondFolder, second.Parent);
    }

    /// <summary>
    /// Names can be supplied while batch import defers sibling validation.
    /// </summary>
    [Fact]
    public void NamesDuringSuspendedValidation()
    {
        SolutionModel solution = new SolutionModel();
        using (solution.SuspendProjectValidation())
        {
            SolutionProjectModel first = solution.AddProject(Path.Join("app1", "Cargo.toml"), ProjectType);
            SolutionProjectModel second = solution.AddProject(Path.Join("app2", "Cargo.toml"), ProjectType);
            first.DisplayNameOverride = "app1";
            second.DisplayNameOverride = "app2";
        }

        Assert.Equal("app1", solution.SolutionProjects[0].ActualDisplayName);
        Assert.Equal("app2", solution.SolutionProjects[1].ActualDisplayName);
    }

    /// <summary>
    /// The existing XML attribute preserves explicit names through repeated saves.
    /// </summary>
    /// <param name="inFolder">Whether projects share a solution folder rather than the root.</param>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task XmlRoundTripAsync(bool inFolder)
    {
        SolutionModel solution = new SolutionModel();
        SolutionFolderModel? folder = inFolder ? solution.AddFolder("/Applications/") : null;
        string firstPath = Path.Join("app1", "Cargo.toml");
        string secondPath = Path.Join("app2", "Cargo.toml");
        _ = solution.AddProject(firstPath, ProjectType, folder, "app1");
        _ = solution.AddProject(secondPath, ProjectType, folder, "app2");

        (SolutionModel reopened, FileContents contents) = await SaveAndReopenModelAsync(SolutionSerializers.SlnXml, solution);
        Assert.Contains("DisplayName=\"app1\"", contents.FullString);
        Assert.Contains("DisplayName=\"app2\"", contents.FullString);
        Assert.Equal("app1", reopened.FindProject(firstPath)!.ActualDisplayName);
        Assert.Equal("app2", reopened.FindProject(secondPath)!.ActualDisplayName);
        Assert.Equal(folder?.Path, reopened.FindProject(firstPath)!.Parent?.Path);

        (SolutionModel reopenedAgain, FileContents contentsAgain) = await SaveAndReopenModelAsync(SolutionSerializers.SlnXml, reopened);
        Assert.Equal(contents.FullString, contentsAgain.FullString);
        Assert.Equal("app2", reopenedAgain.FindProject(secondPath)!.DisplayNameOverride);
    }

    /// <summary>
    /// An override equal to the current file stem still represents an explicit naming policy.
    /// </summary>
    [Fact]
    public async Task XmlPreservesOverrideEqualToFileStemAsync()
    {
        SolutionModel solution = new SolutionModel();
        _ = solution.AddProject("Project.csproj", null, null, "Project");
        (SolutionModel reopened, FileContents contents) = await SaveAndReopenModelAsync(SolutionSerializers.SlnXml, solution);
        Assert.Contains("DisplayName=\"Project\"", contents.FullString);
        SolutionProjectModel project = Assert.Single(reopened.SolutionProjects);
        project.FilePath = "Renamed.csproj";
        Assert.Equal("Project", project.ActualDisplayName);
    }

    /// <summary>
    /// URI projects continue using their mutable legacy display names after XML reload.
    /// </summary>
    [Fact]
    public async Task XmlPreservesLegacyUriNamesAsync()
    {
        SolutionModel solution = new SolutionModel();
        SolutionProjectModel project = solution.AddProject("https://example.com/site/", ProjectType);
        project.DisplayName = "Website";
        (SolutionModel reopened, FileContents _) = await SaveAndReopenModelAsync(SolutionSerializers.SlnXml, solution);
        SolutionProjectModel reloadedProject = Assert.Single(reopened.SolutionProjects);
        Assert.Null(reloadedProject.DisplayNameOverride);
        reloadedProject.DisplayName = "Renamed";
        Assert.Equal("Renamed", reloadedProject.ActualDisplayName);
    }

    /// <summary>
    /// XML names are available before construction validates sibling names.
    /// </summary>
    /// <param name="firstName">The first explicit name, or null to omit it.</param>
    /// <param name="secondName">The second explicit name, or null to omit it.</param>
    /// <param name="succeeds">Whether the effective names are unique.</param>
    [Theory]
    [InlineData("app1", "app2", true)]
    [InlineData("app", "APP", false)]
    [InlineData(null, null, false)]
    public async Task XmlValidatesExplicitNamesAsync(string? firstName, string? secondName, bool succeeds)
    {
        string firstAttribute = firstName is null ? string.Empty : $" DisplayName=\"{firstName}\"";
        string secondAttribute = secondName is null ? string.Empty : $" DisplayName=\"{secondName}\"";
        string xml = $"""
            <Solution>
              <Project Path="app1/Cargo.toml" Type="{ProjectType}"{firstAttribute} />
              <Project Path="app2/Cargo.toml" Type="{ProjectType}"{secondAttribute} />
            </Solution>
            """;
        using MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));
        if (succeeds)
        {
            SolutionModel solution = await SolutionSerializers.SlnXml.OpenAsync(stream, CancellationToken.None);
            Assert.Equal("app1", solution.SolutionProjects[0].ActualDisplayName);
            Assert.Equal("app2", solution.SolutionProjects[1].ActualDisplayName);
        }
        else
        {
            SolutionException exception = await Assert.ThrowsAsync<SolutionException>(() =>
                SolutionSerializers.SlnXml.OpenAsync(stream, CancellationToken.None));
            Assert.Equal(SolutionErrorType.DuplicateProjectName, Assert.IsType<SolutionArgumentException>(exception.InnerException).Type);
        }
    }
}
