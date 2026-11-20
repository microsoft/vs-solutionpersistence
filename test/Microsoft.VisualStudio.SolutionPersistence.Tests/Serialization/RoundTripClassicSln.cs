// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Serialization;

/// <summary>
/// These tests validate SLN files can be round-tripped through the serializer and model.
/// </summary>
[SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1600:Elements should be documented", Justification = "All tests in this class have the same purpose.")]
public class RoundTripClassicSln
{
    /// <summary>
    /// Gets the classic SLN files to round-trip.
    /// </summary>
    /// <returns>The available classic SLN files.</returns>
    public static IEnumerable<ResourceName> ClassicSlnFiles() => SlnAssets.ClassicSlnFiles;

    [Test]
    public Task BlankAsync() => TestRoundTripSerializerAsync(SlnAssets.ClassicSlnBlank);

    [Test]
    public Task CpsAsync() => TestRoundTripSerializerAsync(SlnAssets.ClassicSlnCps);

    [Test]
    public Task EverythingAsync() => TestRoundTripSerializerAsync(SlnAssets.ClassicSlnEverything);

    [Test]
    public Task ManyAsync() => TestRoundTripSerializerAsync(SlnAssets.ClassicSlnMany);

    [Test]
    public Task OrchardCoreAsync() => TestRoundTripSerializerAsync(SlnAssets.ClassicSlnOrchardCore);

    [Test]
    public Task SingleNativeProjectAsync() => TestRoundTripSerializerAsync(SlnAssets.ClassicSlnSingleNativeProject);

    [Test]
    public Task GiantAsync() => TestRoundTripSerializerAsync(SlnAssets.ClassicSlnGiant);

    [Test]
    public Task TraditionalAsync() => TestRoundTripSerializerAsync(SlnAssets.ClassicSlnTraditional);

    [Test]
    public Task MissingConfigurationsAsync() => TestRoundTripSerializerAsync(SlnAssets.ClassicSlnMissingConfigurations);

    [Test]
    public Task FolderIdAsync() => TestRoundTripSerializerAsync(SlnAssets.LoadResource("FolderId.sln"));

    [Test]
    public Task ReportProjectAsync() => TestRoundTripSerializerAsync(SlnAssets.LoadResource("Report Project.sln"));

    [Test]
    [MethodDataSource(nameof(ClassicSlnFiles))]
    public Task AllClassicSolutionAsync(ResourceName sampleFile)
    {
        return TestRoundTripSerializerAsync(sampleFile.Load());
    }

    /// <summary>
    /// Round trip a .SLN file through the serializer and model.
    /// </summary>
    /// <param name="slnStream">The .SLN to test.</param>
    /// <returns>Task to track the asynchronous call status.</returns>
    private static async Task TestRoundTripSerializerAsync(ResourceStream slnStream)
    {
        FileContents originalSolution = slnStream.ToLines();

        // Open the Model from stream.
        SolutionModel model = await SolutionSerializers.SlnFileV12.OpenAsync(slnStream.Stream, CancellationToken.None);
        AssertNotTarnished(model);

        // Save the Model back to stream.
        FileContents reserializedSolution = await model.ToLinesAsync(SolutionSerializers.SlnFileV12);

        AssertSolutionsAreEqual(originalSolution, reserializedSolution);
    }
}
