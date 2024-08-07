// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.VisualStudio.SolutionPersistence;
using Microsoft.VisualStudio.SolutionPersistence.Model;
using Xunit;

namespace Serialization;

/// <summary>
/// Tests for validating the solution model.
/// </summary>
public class Validation
{
    [Fact]
    public void DuplicateProjects()
    {
        // Create a solution with duplicate projects.
        SolutionModel solution = new SolutionModel();
        _ = solution.AddProject("Project.csproj");

        _ = Assert.ThrowsAny<ArgumentException>(() => solution.AddProject("Project.csproj"));
    }

    [Fact]
    public void DuplicateProjectNamesInRoot()
    {
        // Create a solution with duplicate project names, but in different folders.
        SolutionModel solution = new SolutionModel();
        _ = solution.AddProject(Path.Join("Folder1", "Project.csproj"));

        _ = Assert.ThrowsAny<ArgumentException>(() => solution.AddProject(Path.Join("Folder2", "Project.csproj")));
    }

    [Fact]
    public void DuplicateProjectNamesInSameFolder()
    {
        // Create a solution with duplicate project names, but in different folders.
        SolutionModel solution = new SolutionModel();
        SolutionFolderModel folder = solution.AddFolder("/Folder/");
        _ = solution.AddProject(Path.Join("Folder1", "Project.csproj"), folder: folder);

        _ = Assert.ThrowsAny<ArgumentException>(() => solution.AddProject(Path.Join("Folder2", "Project.csproj"), folder: folder));
    }

    [Fact]
    public void DuplicateProjectNamesInDifferentFolder()
    {
        // Create a solution with duplicate project names, but in different folders.
        SolutionModel solution = new SolutionModel();

        SolutionFolderModel folder1 = solution.AddFolder("/Folder1/");
        Assert.NotNull(solution.AddProject(Path.Join("Folder1", "Project.csproj"), folder: folder1));

        SolutionFolderModel folder2 = solution.AddFolder("/Folder2/");
        Assert.NotNull(solution.AddProject(Path.Join("Folder2", "Project.csproj"), folder: folder2));
    }

    [Fact]
    public void SolutionFolders()
    {
        // Create a solution with duplicate solution folders.
        SolutionModel solution = new SolutionModel();

        string invalidNameError = Errors.InvalidName;

        // Don't allow invalid characters
        Assert.StartsWith(invalidNameError, Assert.ThrowsAny<ArgumentException>(() => solution.AddFolder("/Foo#/")).Message);

        // Don't allow reserved names
        Assert.StartsWith(invalidNameError, Assert.ThrowsAny<ArgumentException>(() => solution.AddFolder("/LPT4/")).Message);
        Assert.StartsWith(invalidNameError, Assert.ThrowsAny<ArgumentException>(() => solution.AddFolder("/Aux/")).Message);
        Assert.StartsWith(invalidNameError, Assert.ThrowsAny<ArgumentException>(() => solution.AddFolder("/../")).Message);

        // Verify the maximum length of a folder name
        string longName = "/" + new string('v', 260) + "/";
        _ = solution.AddFolder(longName);
        _ = solution.AddFolder(longName + "after/");
        _ = solution.AddFolder("/before" + longName);

        // Don't allow long names
        string tooLongName = "/" + new string('v', 261) + "/";
        _ = Assert.ThrowsAny<ArgumentOutOfRangeException>(() => solution.AddFolder(tooLongName));
        _ = Assert.ThrowsAny<ArgumentOutOfRangeException>(() => solution.AddFolder(tooLongName + "after/"));
        _ = Assert.ThrowsAny<ArgumentOutOfRangeException>(() => solution.AddFolder("/before" + tooLongName));

        // Don't allow backslash
        _ = Assert.ThrowsAny<ArgumentException>(() => solution.AddFolder("/Foo\\Bar/"));

        // Don't allow double slashed
        _ = Assert.ThrowsAny<ArgumentNullException>(() => solution.AddFolder("/Foo//Bar/")).Message;
        _ = Assert.ThrowsAny<ArgumentNullException>(() => solution.AddFolder("//Foo/Bar/"));
        _ = Assert.ThrowsAny<ArgumentNullException>(() => solution.AddFolder("/Foo/Bar//"));

        // Don't allow path without slashes
        string slashError = string.Format(Errors.InvalidFolderPath_Args1, "Foo");
        Assert.StartsWith(slashError, Assert.ThrowsAny<ArgumentException>(() => solution.AddFolder("Foo")).Message);

        // Folders added as ItemId's try to find existing folders.
        SolutionFolderModel subFolder1 = solution.AddFolder("/Folder/Subfolder/");
        SolutionFolderModel subFolder2 = solution.AddFolder("/Folder/Subfolder/");
        Assert.Equal(subFolder1, subFolder2);

        // Validate internal CreateFolder API for reading .SLN files.
        // This supports loading from SLN files where the folder name is the only thing known.
        SolutionFolderModel folder1 = solution.CreateFolder("UniqueFolder");
        SolutionFolderModel folder2 = solution.CreateFolder("UniqueFolder");
        Assert.NotEqual(folder1, folder2);
    }
}
