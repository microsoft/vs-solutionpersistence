// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.VisualStudio.SolutionPersistence.Model;
using Microsoft.VisualStudio.SolutionPersistence.Serializer;
using Utilities;
using Xunit;
using static Utilities.SlnTestHelper;

namespace Serialization;

/// <summary>
/// These tests validate SLN files can be round-tripped through the serializers and models.
/// Some tests also validate the SLNX models/serializers can be used to round-trip SLN files.
/// </summary>
public class RoundTripClassicSln
{
    public static TheoryData<ResourceStream> ClassicSlnFiles =>
        new TheoryData<ResourceStream>(SlnAssets.ClassicSlnFiles);

    #region Basic sln -> sln round trip

    [Fact]
    public Task BlankAsync() => TestRoundTripSerializerAsync(SlnAssets.ClassicSlnBlank);

    [Fact]
    public Task CpsAsync() => TestRoundTripSerializerAsync(SlnAssets.ClassicSlnCps);

    [Fact]
    public Task EverythingAsync() => TestRoundTripSerializerAsync(SlnAssets.ClassicSlnEverything);

    [Fact]
    public Task OrchardCoreAsync() => TestRoundTripSerializerAsync(SlnAssets.ClassicSlnOrchardCore);

    [Fact]
    public Task SingleNativeProjectAsync() => TestRoundTripSerializerAsync(SlnAssets.ClassicSlnSingleNativeProject);

    #endregion

    [Theory]
    [MemberData(nameof(ClassicSlnFiles))]
    public Task AllClassicSolutionAsync(ResourceStream sampleFile)
    {
        return TestRoundTripSerializerAsync(sampleFile);
    }

    #region sln -> slnx file -> sln round trip

    [Fact]
    public Task BlankThruSlnxStreamAsync() => TestRoundTripSerializerAsync(SlnAssets.ClassicSlnBlank, SlnAssets.XmlSlnxBlank);

    [Fact]
    public Task CpsThruSlnxStreamAsync() => TestRoundTripSerializerAsync(SlnAssets.ClassicSlnCps, SlnAssets.XmlSlnxCps);

    [Fact]
    public Task EverythingThruSlnxStreamAsync() => TestRoundTripSerializerAsync(SlnAssets.ClassicSlnEverything, SlnAssets.XmlSlnxEverything);

    [Fact]
    public Task OrchardCoreThruSlnxStreamAsync() => TestRoundTripSerializerAsync(SlnAssets.ClassicSlnOrchardCore, SlnAssets.XmlSlnxOrchardCore);

    [Fact]
    public Task SingleNativeProjectThruSlnxStreamAsync() => TestRoundTripSerializerAsync(SlnAssets.ClassicSlnSingleNativeProject, SlnAssets.XmlSlnxSingleNativeProject);

    [Fact]
    public Task ManyThruSlnxStreamAsync() => TestRoundTripSerializerAsync(SlnAssets.ClassicSlnMany, SlnAssets.XmlSlnxMany);

    #endregion

    /// <summary>
    /// Round trip a .SLN file through the serializer and model.
    /// </summary>
    /// <param name="slnStream">The .SLN to test.</param>
    /// <param name="viaSlnxStream">When set, saves the .SLN as a .SLNX and reload. This provides the expected .SLNX file.</param>
    /// <returns>Task to track the asynchronous call status.</returns>
    private static async Task TestRoundTripSerializerAsync(
        ResourceStream slnStream,
        ResourceStream? viaSlnxStream = null)
    {
        FileContents originalSolution = slnStream.ToLines();

        // Open the Model from stream.
        SolutionModel model = await SolutionSerializers.SlnFileV12.OpenAsync(slnStream.Name, slnStream.Stream, CancellationToken.None);

        if (viaSlnxStream is not null)
        {
            (model, (string FullString, List<string> Lines) slnxContents) = await ThruSlnStreamAsync(model, slnStream.Name, originalSolution.FullString.Length * 10);

            AssertSolutionsAreEqual(viaSlnxStream.Value.ToLines(), slnxContents);
        }

        // Save the Model back to stream.
        FileContents reserializedSolution = await ModelToLinesAsync(SolutionSerializers.SlnFileV12, model, slnStream.Name);

        AssertSolutionsAreEqual(originalSolution, reserializedSolution);
    }
}
