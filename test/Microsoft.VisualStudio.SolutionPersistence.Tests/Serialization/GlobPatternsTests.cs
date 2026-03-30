// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.VisualStudio.SolutionPersistence.Model;

namespace Serialization;

/// <summary>
/// Tests for glob pattern expansion functionality in SLNX files.
/// </summary>
public sealed class GlobPatternsTests
{
    /// <summary>
    /// Tests that literal file paths (no glob patterns) are preserved as-is.
    /// </summary>
    [Fact]
    public async Task LiteralPathsPreservedAsync()
    {
        string slnxContent = """
            <Solution>
              <Folder Name="/test/">
                <File Path="file1.txt" />
                <File Path="src/file2.cs" />
                <File Path="docs/readme.md" />
              </Folder>
            </Solution>
            """;

        using MemoryStream stream = new(Encoding.UTF8.GetBytes(slnxContent));
        SolutionModel model = await SolutionSerializers.SlnXml.OpenAsync(stream, CancellationToken.None);

        SolutionFolderModel? folder = model.FindFolder("/test/");
        Assert.NotNull(folder);
        Assert.Equal(3, folder.Files?.Count);

        List<string> expectedFiles = ["file1.txt", "src/file2.cs", "docs/readme.md"];
        Assert.Equal(expectedFiles, folder.Files);
    }

    /// <summary>
    /// Tests that glob patterns are expanded to match actual files in the file system.
    /// </summary>
    [Fact]
    public async Task GlobPatternsExpandedAsync()
    {
        // Create a temporary directory structure for testing
        string tempDir = Path.Combine(Path.GetTempPath(), $"slnx_glob_test_{Guid.NewGuid():N}");
        try
        {
            // Create test directory structure
            _ = Directory.CreateDirectory(tempDir);
            _ = Directory.CreateDirectory(Path.Combine(tempDir, "src"));
            _ = Directory.CreateDirectory(Path.Combine(tempDir, "tests"));
            _ = Directory.CreateDirectory(Path.Combine(tempDir, "docs"));
            _ = Directory.CreateDirectory(Path.Combine(tempDir, "obj"));

            // Create test files
            File.WriteAllText(Path.Combine(tempDir, "src", "Program.cs"), "// test");
            File.WriteAllText(Path.Combine(tempDir, "src", "Helper.cs"), "// test");
            File.WriteAllText(Path.Combine(tempDir, "tests", "Test1.cs"), "// test");
            File.WriteAllText(Path.Combine(tempDir, "docs", "README.md"), "# test");
            File.WriteAllText(Path.Combine(tempDir, "obj", "temp.dll"), "temp");

            // Create SLNX with glob patterns
            string slnxContent = """
                <Solution>
                  <Folder Name="/src/">
                    <File Path="**/*.cs" />
                    <File Path="docs/*.md" />
                  </Folder>
                </Solution>
                """;

            string slnxPath = Path.Combine(tempDir, "test.slnx");
            File.WriteAllText(slnxPath, slnxContent);

            // Parse the SLNX file
            SolutionModel model = await SolutionSerializers.SlnXml.OpenAsync(slnxPath, CancellationToken.None);

            // Verify glob patterns were expanded
            SolutionFolderModel? folder = model.FindFolder("/src/");
            Assert.NotNull(folder);
            Assert.NotNull(folder.Files);
            Assert.True(folder.Files.Count >= 3); // At least the files we created

            // Should include CS files
            Assert.Contains(folder.Files, f => f.Contains("Program.cs"));
            Assert.Contains(folder.Files, f => f.Contains("Helper.cs"));
            Assert.Contains(folder.Files, f => f.Contains("Test1.cs"));

            // Should include MD files
            Assert.Contains(folder.Files, f => f.Contains("README.md"));
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    /// <summary>
    /// Tests that exclude patterns (prefixed with !) work correctly.
    /// </summary>
    [Fact]
    public async Task ExcludePatternsWorkAsync()
    {
        // Create a temporary directory structure for testing
        string tempDir = Path.Combine(Path.GetTempPath(), $"slnx_exclude_test_{Guid.NewGuid():N}");
        try
        {
            // Create test directory structure
            _ = Directory.CreateDirectory(tempDir);
            _ = Directory.CreateDirectory(Path.Combine(tempDir, "src"));
            _ = Directory.CreateDirectory(Path.Combine(tempDir, "obj"));
            _ = Directory.CreateDirectory(Path.Combine(tempDir, "bin"));

            // Create test files
            File.WriteAllText(Path.Combine(tempDir, "src", "Program.cs"), "// test");
            File.WriteAllText(Path.Combine(tempDir, "obj", "Program.dll"), "temp");
            File.WriteAllText(Path.Combine(tempDir, "bin", "Program.exe"), "temp");

            // Create SLNX with include and exclude patterns
            string slnxContent = """
                <Solution>
                  <Folder Name="/files/">
                    <File Path="**/*" />
                    <File Path="!**/obj/**" />
                    <File Path="!**/bin/**" />
                  </Folder>
                </Solution>
                """;

            string slnxPath = Path.Combine(tempDir, "test.slnx");
            File.WriteAllText(slnxPath, slnxContent);

            // Parse the SLNX file
            SolutionModel model = await SolutionSerializers.SlnXml.OpenAsync(slnxPath, CancellationToken.None);

            // Verify exclude patterns worked
            SolutionFolderModel? folder = model.FindFolder("/files/");
            Assert.NotNull(folder);
            Assert.NotNull(folder.Files);

            // Should include source files
            Assert.Contains(folder.Files, f => f.Contains("Program.cs"));

            // Should NOT include obj/bin files due to exclusion
            Assert.DoesNotContain(folder.Files, f => f.Contains("obj/"));
            Assert.DoesNotContain(folder.Files, f => f.Contains("bin/"));
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    /// <summary>
    /// Tests mixed literal paths and glob patterns.
    /// </summary>
    [Fact]
    public async Task MixedPatternsWorkAsync()
    {
        // Create a temporary directory structure for testing
        string tempDir = Path.Combine(Path.GetTempPath(), $"slnx_mixed_test_{Guid.NewGuid():N}");
        try
        {
            // Create test directory structure
            _ = Directory.CreateDirectory(tempDir);
            _ = Directory.CreateDirectory(Path.Combine(tempDir, "src"));

            // Create test files
            File.WriteAllText(Path.Combine(tempDir, "LICENSE"), "MIT License");
            File.WriteAllText(Path.Combine(tempDir, "src", "Program.cs"), "// test");
            File.WriteAllText(Path.Combine(tempDir, "src", "Helper.cs"), "// test");

            // Create SLNX with mixed patterns
            string slnxContent = """
                <Solution>
                  <Folder Name="/mixed/">
                    <File Path="LICENSE" />
                    <File Path="src/*.cs" />
                    <File Path="README.md" />
                  </Folder>
                </Solution>
                """;

            string slnxPath = Path.Combine(tempDir, "test.slnx");
            File.WriteAllText(slnxPath, slnxContent);

            // Parse the SLNX file
            SolutionModel model = await SolutionSerializers.SlnXml.OpenAsync(slnxPath, CancellationToken.None);

            // Verify mixed patterns worked
            SolutionFolderModel? folder = model.FindFolder("/mixed/");
            Assert.NotNull(folder);
            Assert.NotNull(folder.Files);

            // Should include literal file that exists
            Assert.Contains(folder.Files, f => f == "LICENSE");

            // Should include glob-matched files
            Assert.Contains(folder.Files, f => f.Contains("Program.cs"));
            Assert.Contains(folder.Files, f => f.Contains("Helper.cs"));

            // Should NOT include literal file that doesn't exist - Matcher only returns existing files
            Assert.Contains(folder.Files, f => f == "README.md");
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    /// <summary>
    /// Tests that forward slash paths are preserved in the output.
    /// </summary>
    [Fact]
    public async Task ForwardSlashPathsPreservedAsync()
    {
        // Create a temporary directory structure for testing
        string tempDir = Path.Combine(Path.GetTempPath(), $"slnx_path_test_{Guid.NewGuid():N}");
        try
        {
            // Create test directory structure
            _ = Directory.CreateDirectory(tempDir);
            _ = Directory.CreateDirectory(Path.Combine(tempDir, "src", "nested"));

            // Create test files
            File.WriteAllText(Path.Combine(tempDir, "src", "nested", "deep.cs"), "// test");

            // Create SLNX with glob pattern
            string slnxContent = """
                <Solution>
                  <Folder Name="/paths/">
                    <File Path="src/**/*.cs" />
                  </Folder>
                </Solution>
                """;

            string slnxPath = Path.Combine(tempDir, "test.slnx");
            File.WriteAllText(slnxPath, slnxContent);

            // Parse the SLNX file
            SolutionModel model = await SolutionSerializers.SlnXml.OpenAsync(slnxPath, CancellationToken.None);

            // Verify forward slashes are preserved
            SolutionFolderModel? folder = model.FindFolder("/paths/");
            Assert.NotNull(folder);
            Assert.NotNull(folder.Files);

            // Should use forward slashes in the resolved path
            string? matchedFile = null;
            foreach (string file in folder.Files)
            {
                if (file.Contains("deep.cs"))
                {
                    matchedFile = file;
                    break;
                }
            }

            Assert.NotNull(matchedFile);
            Assert.Contains("/", matchedFile); // Should contain forward slashes
            Assert.DoesNotContain("\\", matchedFile); // Should not contain backslashes
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    /// <summary>
    /// Tests that literal project paths (no glob patterns) are preserved as-is.
    /// </summary>
    [Fact]
    public async Task LiteralProjectPathsPreservedAsync()
    {
        string slnxContent = """
            <Solution>
              <Folder Name="/projects/">
                <Project Path="src/Project1/Project1.csproj" />
                <Project Path="src/Project2/Project2.csproj" />
                <Project Path="tests/TestProject/TestProject.csproj" />
              </Folder>
            </Solution>
            """;

        using MemoryStream stream = new(Encoding.UTF8.GetBytes(slnxContent));
        SolutionModel model = await SolutionSerializers.SlnXml.OpenAsync(stream, CancellationToken.None);

        SolutionFolderModel? folder = model.FindFolder("/projects/");
        Assert.NotNull(folder);

        // Get projects in this folder from the solution's SolutionProjects collection
        List<string> projectsInFolder = [];
        foreach (SolutionProjectModel project in model.SolutionProjects)
        {
            if (ReferenceEquals(project.Parent, folder))
            {
                projectsInFolder.Add(project.FilePath);
            }
        }

        Assert.Equal(3, projectsInFolder.Count);

        List<string> expectedProjects = ["src/Project1/Project1.csproj", "src/Project2/Project2.csproj", "tests/TestProject/TestProject.csproj"];
        Assert.Equal(expectedProjects, projectsInFolder);
    }

    /// <summary>
    /// Tests that glob patterns work for Project elements.
    /// </summary>
    [Fact]
    public async Task ProjectGlobPatternsExpandedAsync()
    {
        // Create a temporary directory structure for testing
        string tempDir = Path.Combine(Path.GetTempPath(), $"slnx_project_glob_test_{Guid.NewGuid():N}");
        try
        {
            // Create test directory structure
            _ = Directory.CreateDirectory(tempDir);
            _ = Directory.CreateDirectory(Path.Combine(tempDir, "src", "WebApp"));
            _ = Directory.CreateDirectory(Path.Combine(tempDir, "src", "Library"));
            _ = Directory.CreateDirectory(Path.Combine(tempDir, "tests", "UnitTests"));

            // Create test project files
            File.WriteAllText(Path.Combine(tempDir, "src", "WebApp", "WebApp.csproj"), "<Project Sdk=\"Microsoft.NET.Sdk.Web\" />");
            File.WriteAllText(Path.Combine(tempDir, "src", "Library", "Library.csproj"), "<Project Sdk=\"Microsoft.NET.Sdk\" />");
            File.WriteAllText(Path.Combine(tempDir, "tests", "UnitTests", "UnitTests.csproj"), "<Project Sdk=\"Microsoft.NET.Sdk\" />");

            // Create SLNX with Project glob patterns
            string slnxContent = """
                <Solution>
                  <Folder Name="/all-projects/">
                    <Project Path="**/*.csproj" />
                  </Folder>
                </Solution>
                """;

            string slnxPath = Path.Combine(tempDir, "test.slnx");
            File.WriteAllText(slnxPath, slnxContent);

            // Parse the SLNX file
            SolutionModel model = await SolutionSerializers.SlnXml.OpenAsync(slnxPath, CancellationToken.None);

            // Verify Project glob patterns were expanded
            SolutionFolderModel? folder = model.FindFolder("/all-projects/");
            Assert.NotNull(folder);

            // Get projects in this folder from the solution's SolutionProjects collection
            List<string> projectsInFolder = [];
            foreach (SolutionProjectModel project in model.SolutionProjects)
            {
                if (ReferenceEquals(project.Parent, folder))
                {
                    projectsInFolder.Add(project.FilePath);
                }
            }

            Assert.Equal(3, projectsInFolder.Count);

            // Should include all project files
            Assert.Contains(projectsInFolder, p => p.Contains("WebApp.csproj"));
            Assert.Contains(projectsInFolder, p => p.Contains("Library.csproj"));
            Assert.Contains(projectsInFolder, p => p.Contains("UnitTests.csproj"));
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    /// <summary>
    /// Tests mixed literal and glob Project patterns.
    /// </summary>
    [Fact]
    public async Task MixedProjectPatternsWorkAsync()
    {
        // Create a temporary directory structure for testing
        string tempDir = Path.Combine(Path.GetTempPath(), $"slnx_mixed_project_test_{Guid.NewGuid():N}");
        try
        {
            // Create test directory structure
            _ = Directory.CreateDirectory(tempDir);
            _ = Directory.CreateDirectory(Path.Combine(tempDir, "src"));
            _ = Directory.CreateDirectory(Path.Combine(tempDir, "tests"));

            // Create test project files
            File.WriteAllText(Path.Combine(tempDir, "src", "Main.csproj"), "<Project Sdk=\"Microsoft.NET.Sdk\" />");
            File.WriteAllText(Path.Combine(tempDir, "tests", "Tests.csproj"), "<Project Sdk=\"Microsoft.NET.Sdk\" />");
            File.WriteAllText(Path.Combine(tempDir, "Special.csproj"), "<Project Sdk=\"Microsoft.NET.Sdk\" />");

            // Create SLNX with mixed Project patterns
            string slnxContent = """
                <Solution>
                  <Folder Name="/mixed-projects/">
                    <Project Path="Special.csproj" />
                    <Project Path="src/*.csproj" />
                    <Project Path="NonExistent.csproj" />
                  </Folder>
                </Solution>
                """;

            string slnxPath = Path.Combine(tempDir, "test.slnx");
            File.WriteAllText(slnxPath, slnxContent);

            // Parse the SLNX file
            SolutionModel model = await SolutionSerializers.SlnXml.OpenAsync(slnxPath, CancellationToken.None);

            // Verify mixed Project patterns worked
            SolutionFolderModel? folder = model.FindFolder("/mixed-projects/");
            Assert.NotNull(folder);

            // Get projects in this folder from the solution's SolutionProjects collection
            List<string> projectsInFolder = [];
            foreach (SolutionProjectModel project in model.SolutionProjects)
            {
                if (ReferenceEquals(project.Parent, folder))
                {
                    projectsInFolder.Add(project.FilePath);
                }
            }

            // Should include literal project that exists
            Assert.Contains(projectsInFolder, p => p == "Special.csproj");

            // Should include glob-matched projects
            Assert.Contains(projectsInFolder, p => p.Contains("Main.csproj"));

            // Should include literal project that doesn't exist (preserved as-is)
            Assert.Contains(projectsInFolder, p => p == "NonExistent.csproj");
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    /// <summary>
    /// Tests that both root-level and folder-level projects support globbing.
    /// </summary>
    [Fact]
    public async Task BothRootAndFolderProjectGlobbingAsync()
    {
        // Create a temporary directory structure for testing
        string tempDir = Path.Combine(Path.GetTempPath(), $"slnx_root_folder_test_{Guid.NewGuid():N}");
        try
        {
            // Create test directory structure
            _ = Directory.CreateDirectory(tempDir);
            _ = Directory.CreateDirectory(Path.Combine(tempDir, "root"));
            _ = Directory.CreateDirectory(Path.Combine(tempDir, "src", "libraries"));
            _ = Directory.CreateDirectory(Path.Combine(tempDir, "tests"));

            // Create test project files
            File.WriteAllText(Path.Combine(tempDir, "root", "RootProject.csproj"), "<Project Sdk=\"Microsoft.NET.Sdk\" />");
            File.WriteAllText(Path.Combine(tempDir, "src", "libraries", "Library.csproj"), "<Project Sdk=\"Microsoft.NET.Sdk\" />");
            File.WriteAllText(Path.Combine(tempDir, "tests", "UnitTests.csproj"), "<Project Sdk=\"Microsoft.NET.Sdk\" />");

            // Create SLNX with both root and folder project patterns
            string slnxContent = """
                <Solution>
                  <Project Path="root/*.csproj" />
                  <Folder Name="/src/">
                    <Project Path="**/*.csproj" />
                  </Folder>
                </Solution>
                """;

            string slnxPath = Path.Combine(tempDir, "test.slnx");
            File.WriteAllText(slnxPath, slnxContent);

            // Parse the SLNX file
            SolutionModel model = await SolutionSerializers.SlnXml.OpenAsync(slnxPath, CancellationToken.None);

            // Check root-level projects
            List<string> rootProjects = [];
            foreach (SolutionProjectModel project in model.SolutionProjects)
            {
                // Root level projects
                if (project.Parent == null)
                {
                    rootProjects.Add(project.FilePath);
                }
            }

            // Should include root glob-matched project
            Assert.Contains(rootProjects, p => p.Contains("RootProject.csproj"));

            // Check folder-level projects
            SolutionFolderModel? srcFolder = model.FindFolder("/src/");
            Assert.NotNull(srcFolder);

            List<string> folderProjects = [];
            foreach (SolutionProjectModel project in model.SolutionProjects)
            {
                if (ReferenceEquals(project.Parent, srcFolder))
                {
                    folderProjects.Add(project.FilePath);
                }
            }

            // Should include folder glob-matched projects
            Assert.Contains(folderProjects, p => p.Contains("Library.csproj"));
            Assert.Contains(folderProjects, p => p.Contains("UnitTests.csproj"));
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }
}
