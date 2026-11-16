// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Serialization;

/// <summary>
/// Tests project-type exemptions from duplicate sibling-name validation.
/// </summary>
public sealed class DuplicateProjectNames
{
    private const string RustType = "0D916ADF-294A-4D07-A543-8F58229C3415";
    private const string CSharpType = "FAE04EC0-301F-11D3-BF4B-00C04F79EFBC";

    /// <summary>
    /// Explicit, named, inherited and extension-inferred Rust types allow duplicate names.
    /// </summary>
    /// <param name="type">The project type reference.</param>
    /// <param name="inFolder">Whether to add projects to a solution folder.</param>
    [Theory]
    [InlineData(RustType, false)]
    [InlineData(RustType, true)]
    [InlineData(null, false)]
    [InlineData(null, true)]
    [InlineData("Rust", false)]
    [InlineData("Rust", true)]
    [InlineData("Inherited", false)]
    [InlineData("Inherited", true)]
    public void RustNames(string? type, bool inFolder)
    {
        SolutionModel solution = CreateSolution();
        SolutionFolderModel? folder = inFolder ? solution.AddFolder("/Applications/") : null;
        SolutionProjectModel first = solution.AddProject(Path.Join("app1", "Cargo.toml"), projectTypeName: type, folder: folder);
        SolutionProjectModel second = solution.AddProject(Path.Join("app2", "Cargo.toml"), projectTypeName: type, folder: folder);

        Assert.Equal(new Guid(RustType), first.TypeId);
        Assert.Equal(first.TypeId, second.TypeId);
        first.DisplayName = "First application";
        second.DisplayName = "Second application";
        Assert.Equal("Cargo", first.ActualDisplayName);
        Assert.Equal("Cargo", second.ActualDisplayName);

        SolutionModel copy = new SolutionModel(solution);
        Assert.Equal(2, copy.SolutionProjects.Count);
        Assert.Equal(new Guid(RustType), copy.SolutionProjects[0].TypeId);
        Assert.Equal("Cargo", copy.SolutionProjects[1].ActualDisplayName);
        Assert.Equal(folder?.Path, copy.SolutionProjects[1].Parent?.Path);
    }

    /// <summary>
    /// Exempt projects do not conflict with ordinary projects in either insertion order.
    /// </summary>
    /// <param name="rustFirst">Whether the Rust project is added first.</param>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void MixedTypes(bool rustFirst)
    {
        SolutionModel solution = CreateSolution();
        string rustPath = Path.Join("rust", "Cargo.toml");
        string csharpPath = Path.Join("csharp", "Cargo.csproj");
        if (rustFirst)
        {
            _ = solution.AddProject(rustPath);
            _ = solution.AddProject(csharpPath);
        }
        else
        {
            _ = solution.AddProject(csharpPath);
            _ = solution.AddProject(rustPath);
        }

        SolutionArgumentException exception = Assert.Throws<SolutionArgumentException>(
            () => solution.AddProject(Path.Join("another", "CARGO.csproj")));
        Assert.Equal(SolutionErrorType.DuplicateProjectName, exception.Type);
        Assert.Equal(2, solution.SolutionProjects.Count);
    }

    /// <summary>
    /// Ordinary and unknown project types retain case-insensitive name validation.
    /// </summary>
    /// <param name="type">The project type reference.</param>
    [Theory]
    [InlineData(null)]
    [InlineData(CSharpType)]
    [InlineData("B1CC274F-733D-46A5-B148-8309670B362D")]
    public void NonExemptNames(string? type)
    {
        SolutionModel solution = new SolutionModel();
        _ = solution.AddProject(Path.Join("first", "Cargo.csproj"), projectTypeName: type);
        SolutionArgumentException exception = Assert.Throws<SolutionArgumentException>(
            () => solution.AddProject(Path.Join("second", "CARGO.csproj"), projectTypeName: type));
        Assert.Equal(SolutionErrorType.DuplicateProjectName, exception.Type);
    }

    /// <summary>
    /// An exempt GUID is still required when projects use a TOML extension.
    /// </summary>
    [Fact]
    public void TomlDoesNotImplyExemption()
    {
        SolutionModel solution = new SolutionModel
        {
            ProjectTypes = [new ProjectType(new Guid(CSharpType), []) { Extension = ".toml" }],
        };
        _ = solution.AddProject(Path.Join("first", "Cargo.toml"));
        SolutionArgumentException exception = Assert.Throws<SolutionArgumentException>(
            () => solution.AddProject(Path.Join("second", "Cargo.toml")));
        Assert.Equal(SolutionErrorType.DuplicateProjectName, exception.Type);
    }

    /// <summary>
    /// Exempt projects retain global path uniqueness when added or renamed.
    /// </summary>
    [Fact]
    public void DuplicatePaths()
    {
        SolutionModel solution = CreateSolution();
        SolutionProjectModel first = solution.AddProject(Path.Join("first", "Cargo.toml"));
        SolutionProjectModel second = solution.AddProject(Path.Join("second", "Cargo.toml"));
        SolutionFolderModel folder = solution.AddFolder("/Other/");

        SolutionArgumentException addException = Assert.Throws<SolutionArgumentException>(
            () => solution.AddProject(first.FilePath, folder: folder));
        Assert.Equal(SolutionErrorType.DuplicateItemRef, addException.Type);
        string oldPath = second.FilePath;
        _ = Assert.Throws<SolutionArgumentException>(() => second.FilePath = first.FilePath);
        Assert.Equal(oldPath, second.FilePath);
        Assert.Equal(2, solution.SolutionProjects.Count);
    }

    /// <summary>
    /// Renames and folder moves use the exemption but still validate ordinary names.
    /// </summary>
    [Fact]
    public void RenameAndMove()
    {
        SolutionModel solution = CreateSolution();
        SolutionFolderModel folder = solution.AddFolder("/Other/");
        _ = solution.AddProject(Path.Join("first", "Cargo.toml"));
        SolutionProjectModel rust = solution.AddProject(Path.Join("second", "Other.toml"), folder: folder);
        rust.FilePath = Path.Join("second", "Cargo.toml");
        rust.MoveToFolder(folder: null);
        Assert.Null(rust.Parent);
        Assert.Equal("Cargo", rust.ActualDisplayName);

        _ = solution.AddProject(Path.Join("first", "Cargo.csproj"));
        SolutionProjectModel csharp = solution.AddProject(Path.Join("second", "Cargo.csproj"), folder: folder);
        _ = Assert.Throws<SolutionArgumentException>(() => csharp.MoveToFolder(folder: null));
        Assert.Same(folder, csharp.Parent);
        csharp.FilePath = Path.Join("second", "Other.csproj");
        csharp.MoveToFolder(folder: null);
        _ = Assert.Throws<SolutionArgumentException>(() => csharp.FilePath = Path.Join("second", "Cargo.csproj"));
        Assert.Equal("Other", csharp.ActualDisplayName);
    }

    /// <summary>
    /// Type changes validate newly participating names and restore both type fields on failure.
    /// </summary>
    /// <param name="type">A GUID or name for a non-exempt type.</param>
    [Theory]
    [InlineData(CSharpType)]
    [InlineData("C#")]
    public void TypeChanges(string type)
    {
        SolutionModel solution = CreateSolution();
        SolutionProjectModel first = solution.AddProject(Path.Join("first", "Cargo.csproj"));
        SolutionProjectModel second = solution.AddProject(Path.Join("second", "Cargo.csproj"), projectTypeName: RustType);
        string oldType = second.Type;

        SolutionArgumentException exception = Assert.Throws<SolutionArgumentException>(() => second.Type = type);
        Assert.Equal(SolutionErrorType.DuplicateProjectName, exception.Type);
        Assert.Equal(new Guid(RustType), second.TypeId);
        Assert.Equal(oldType, second.Type);
        _ = Assert.Throws<ArgumentNullException>(() => second.Type = Guid.Empty.ToString());
        Assert.Equal(new Guid(RustType), second.TypeId);
        Assert.Equal(oldType, second.Type);

        first.Type = RustType;
        second.Type = type;
        Assert.Equal(new Guid(CSharpType), second.TypeId);
        Assert.Equal("Cargo", second.ActualDisplayName);
    }

    /// <summary>
    /// Deferred validation uses the final type rather than intermediate duplicate names.
    /// </summary>
    [Fact]
    public void DeferredValidation()
    {
        SolutionModel solution = CreateSolution();
        using (solution.SuspendProjectValidation())
        {
            _ = solution.AddProject(Path.Join("first", "Cargo.csproj"));
            SolutionProjectModel second = solution.AddProject(Path.Join("second", "Cargo.csproj"));
            second.Type = RustType;
        }

        Assert.Equal(2, solution.SolutionProjects.Count);
        Assert.Equal(new Guid(RustType), solution.SolutionProjects[1].TypeId);
    }

    /// <summary>
    /// Both file formats retain exempt names and the original display-name policy.
    /// </summary>
    /// <param name="xml">Whether to use the XML serializer.</param>
    /// <param name="inferred">Whether to infer Rust from its extension mapping.</param>
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task RoundTripAsync(bool xml, bool inferred)
    {
        SolutionModel solution = CreateSolution();
        SolutionFolderModel folder = solution.AddFolder("/Applications/");
        string? type = inferred ? null : RustType;
        SolutionProjectModel first = solution.AddProject(Path.Join("first", "Cargo.toml"), projectTypeName: type, folder: folder);
        SolutionProjectModel second = solution.AddProject(Path.Join("second", "Cargo.toml"), projectTypeName: type, folder: folder);
        first.DisplayName = "First application";
        second.DisplayName = "Second application";
        SolutionProjectModel csharp = solution.AddProject("Cargo.csproj", folder: folder);
        csharp.DisplayName = "Ignored";

        using MemoryStream stream = new MemoryStream();
        if (xml)
        {
            await SolutionSerializers.SlnXml.SaveAsync(stream, solution, CancellationToken.None);
            stream.Position = 0;
            solution = await SolutionSerializers.SlnXml.OpenAsync(stream, CancellationToken.None);
        }
        else
        {
            await SolutionSerializers.SlnFileV12.SaveAsync(stream, solution, CancellationToken.None);
            stream.Position = 0;
            solution = await SolutionSerializers.SlnFileV12.OpenAsync(stream, CancellationToken.None);
        }

        Assert.Equal(3, solution.SolutionProjects.Count);
        foreach (SolutionProjectModel project in solution.SolutionProjects)
        {
            Assert.Equal("Cargo", project.ActualDisplayName);
            Assert.Equal("/Applications/", project.Parent?.Path);
        }

        Assert.Equal(new Guid(RustType), solution.FindProject(first.FilePath)!.TypeId);
        Assert.Equal(new Guid(RustType), solution.FindProject(second.FilePath)!.TypeId);
    }

    /// <summary>
    /// XML display names remain descriptive metadata, not file-backed name overrides.
    /// </summary>
    [Fact]
    public async Task XmlDisplayNamesAsync()
    {
        string xml = $"""
            <Solution>
              <Project Path="first/Cargo.toml" Type="{RustType}" DisplayName="First application" />
              <Project Path="second/Cargo.toml" Type="{RustType}" DisplayName="Second application" />
              <Project Path="Cargo.csproj" DisplayName="Ignored" />
              <Project Path="https://example.test/" Type="E24C65DC-7377-472B-9ABA-BC803B73C61A" DisplayName="Website" />
            </Solution>
            """;
        using MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));
        SolutionModel solution = await SolutionSerializers.SlnXml.OpenAsync(stream, CancellationToken.None);
        SolutionProjectModel first = solution.FindProject(Path.Join("first", "Cargo.toml"))!;
        Assert.NotNull(first);
        Assert.Equal("First application", first.DisplayName);
        Assert.Equal("Cargo", first.ActualDisplayName);
        Assert.Equal("Cargo", solution.FindProject(Path.Join("second", "Cargo.toml"))!.ActualDisplayName);
        Assert.Equal("Cargo", solution.FindProject("Cargo.csproj")!.ActualDisplayName);
        Assert.Equal("Website", solution.FindProject("https://example.test/")!.ActualDisplayName);
    }

    private static SolutionModel CreateSolution()
    {
        return new SolutionModel
        {
            ProjectTypes =
            [
                new ProjectType(new Guid(RustType), []) { Name = "Rust", Extension = ".toml" },
                new ProjectType(Guid.Empty, []) { Name = "Inherited", BasedOn = "Rust" },
            ],
        };
    }
}
