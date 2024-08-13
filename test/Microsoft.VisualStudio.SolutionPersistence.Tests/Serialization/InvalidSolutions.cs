// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Xml;

namespace Serialization;

/// <summary>
/// These tests validate that errors are reported when trying to open invalid or malformed solution files.
/// </summary>
public sealed class InvalidSolutions
{
    // Checks for a file that isn't XML.
    [Fact]
    public Task InvalidXmlSlnxAsync()
    {
        return Assert.ThrowsAsync<XmlException>(async () =>
        {
            using MemoryStream memoryStream = new(Encoding.UTF8.GetBytes("Invalid slnx file"));
            _ = await SolutionSerializers.SlnXml.OpenAsync(memoryStream, CancellationToken.None);
        });
    }

    // Checks for an XML file that isn't an SLNX file.
    [Fact]
    [Trait("TestCategory", "FailsInCloudTest")]
    public async Task InvalidSlnxAsync()
    {
        ResourceStream wrongRoot = SlnAssets.LoadResource(@"Invalid\WrongRoot.slnx");
        string wrongRootFile = wrongRoot.SaveResourceToTempFile();

        SolutionException ex = await Assert.ThrowsAsync<SolutionException>(
            async () => _ = await SolutionSerializers.SlnXml.OpenAsync(wrongRootFile, CancellationToken.None));

        Assert.Equal(Errors.NotSolution, ex.Message);
        Assert.Equal(wrongRootFile, ex.File);

        // This requires additional work with the SLNX serializer, this needs to be captured when deserializing the file.
        Assert.Null(ex.Line);
        Assert.Null(ex.Column);
    }

    // Check for file that isn't an .sln file.
    [Fact]
    [Trait("TestCategory", "FailsInCloudTest")]
    public async Task InvalidSlnAsync()
    {
        ResourceStream invalidSln = SlnAssets.LoadResource(@"Invalid\Invalid.sln");
        string invalidSlnFile = invalidSln.SaveResourceToTempFile();

        SolutionException ex = await Assert.ThrowsAsync<SolutionException>(
            async () => _ = await SolutionSerializers.SlnFileV12.OpenAsync(invalidSlnFile, CancellationToken.None));

        Assert.Equal(Errors.NotSolution, ex.Message);
        Assert.Equal(invalidSlnFile, ex.File);
        Assert.Equal(2, ex.Line);
        Assert.Null(ex.Column);
    }

    // Check for loop in the configuration.
    [Fact]
    public Task InvalidConfigurationLoopAsync()
    {
        ResourceStream basedOnLoop = SlnAssets.LoadResource(@"Invalid\BasedOnLoop.slnx");

        return Assert.ThrowsAsync<SolutionException>(
            async () => _ = await SolutionSerializers.SlnXml.OpenAsync(basedOnLoop.Stream, CancellationToken.None));
    }

    // Check for a .sln missing the end tags.
    [Fact]
    public async Task MissingEndAsync()
    {
        ResourceStream missingEnd = SlnAssets.LoadResource(@"Invalid\MissingEnd.sln");
        SolutionModel solution = await SolutionSerializers.SlnFileV12.OpenAsync(missingEnd.Stream, CancellationToken.None);
        Assert.NotNull(solution.SerializerExtension);
        Assert.True(solution.SerializerExtension.Tarnished);
    }

    // Check that extra lines are ignored.
    [Fact]
    public async Task ExtraLinesAsync()
    {
        ResourceStream extraLines = SlnAssets.LoadResource(@"Invalid\ExtraLines.sln");
        SolutionModel solution = await SolutionSerializers.SlnFileV12.OpenAsync(extraLines.Stream, CancellationToken.None);
        Assert.NotNull(solution.SerializerExtension);
        Assert.False(solution.SerializerExtension.Tarnished);

        // Save the Model back to stream.
        FileContents reserializedSolution = await ModelToLinesAsync(SolutionSerializers.SlnFileV12, solution);

        AssertSolutionsAreEqual(SlnAssets.ClassicSlnMin.ToLines(), reserializedSolution);
    }

    [Fact]
    public async Task SolutionFolderAsync()
    {
        ResourceStream noEndSlash = SlnAssets.LoadResource(@"Invalid\SolutionFolder-NoEndSlash.slnx");
        SolutionException exNoEndSlash = await Assert.ThrowsAsync<SolutionException>(
            async () => _ = await SolutionSerializers.SlnXml.OpenAsync(noEndSlash.Stream, CancellationToken.None));
        Assert.StartsWith(string.Format(Errors.InvalidFolderPath_Args1, @"/No/End/Slash"), exNoEndSlash.Message);

        ResourceStream noStartSlash = SlnAssets.LoadResource(@"Invalid\SolutionFolder-NoStartSlash.slnx");
        SolutionException exNoStartSlash = await Assert.ThrowsAsync<SolutionException>(
            async () => _ = await SolutionSerializers.SlnXml.OpenAsync(noStartSlash.Stream, CancellationToken.None));
        Assert.StartsWith(string.Format(Errors.InvalidFolderPath_Args1, @"No/Start/Slash/"), exNoStartSlash.Message);

        ResourceStream wrongSlash = SlnAssets.LoadResource(@"Invalid\SolutionFolder-WrongSlash.slnx");
        SolutionException exWrongSlash = await Assert.ThrowsAsync<SolutionException>(
            async () => _ = await SolutionSerializers.SlnXml.OpenAsync(wrongSlash.Stream, CancellationToken.None));
        Assert.StartsWith(string.Format(Errors.InvalidName, @"/Wrong\Slash/"), exWrongSlash.Message);
    }
}
