// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.VisualStudio.SolutionPersistence.Model;
using Microsoft.VisualStudio.SolutionPersistence.Serializer;
using Utilities;
using Xunit;
using Xunit.Sdk;
using static Utilities.SlnTestHelper;

namespace Serialization;

/// <summary>
/// These tests validate SLNX files can be round-tripped through the serializers and models.
/// </summary>
public class RoundTripXmlSlnx
{
    public static TheoryData<ResourceStream> XmlSlnxFiles =>
        new TheoryData<ResourceStream>(SlnAssets.XmlSlnxFiles);

    #region Basic slnx -> slnx round trip

    [Fact]
    public Task CommentsAsync() => TestRoundTripSerializerAsync(SlnAssets.XmlSlnxComments);

    [Fact]
    public Task BlankAsync() => TestRoundTripSerializerAsync(SlnAssets.XmlSlnxBlank);

    [Fact]
    public Task CpsAsync() => TestRoundTripSerializerAsync(SlnAssets.XmlSlnxCps);

    [Fact]
    public Task EverythingAsync() => TestRoundTripSerializerAsync(SlnAssets.XmlSlnxEverything);

    [Fact]
    public Task OrchardCoreAsync() => TestRoundTripSerializerAsync(SlnAssets.XmlSlnxOrchardCore);

    [Fact]
    public Task SingleNativeProjectAsync() => TestRoundTripSerializerAsync(SlnAssets.XmlSlnxSingleNativeProject);

    [Fact]
    public Task BuiltInProjectTypesAsync() => TestRoundTripSerializerAsync(SlnAssets.XmlBuiltInProjectTypes);

    #endregion

    [Theory]
    [MemberData(nameof(XmlSlnxFiles))]
    public Task AllXmlSolutionAsync(ResourceStream sampleFile)
    {
        return TestRoundTripSerializerAsync(sampleFile);
    }

    #region slnx -> sln -> slnx round trip

    [Fact]
    public Task CommentsThruBuilderAsync()
    {
        return Assert.ThrowsAsync<FailException>(() => TestRoundTripSerializerAsync(SlnAssets.XmlSlnxComments, thruBuilder: true));
    }

    [Fact]
    public Task BlankThruBuilderAsync() => TestRoundTripSerializerAsync(SlnAssets.XmlSlnxBlank, thruBuilder: true);

    [Fact]
    public Task CpsThruBuilderAsync() => TestRoundTripSerializerAsync(SlnAssets.XmlSlnxCps, thruBuilder: true);

    [Fact]
    public Task EverythingThruBuilderAsync() => TestRoundTripSerializerAsync(SlnAssets.XmlSlnxEverything, thruBuilder: true);

    [Fact]
    public Task OrchardCoreThruBuilderAsync() => TestRoundTripSerializerAsync(SlnAssets.XmlSlnxOrchardCore, thruBuilder: true);

    [Fact]
    public Task SingleNativeProjectThruBuilderAsync() => TestRoundTripSerializerAsync(SlnAssets.XmlSlnxSingleNativeProject, thruBuilder: true);

    [Fact]
    public Task BuiltInProjectTypesThruBuilderAsync() => TestRoundTripSerializerAsync(SlnAssets.XmlBuiltInProjectTypes, thruBuilder: true);

    #endregion

    private static async Task TestRoundTripSerializerAsync(ResourceStream slnStream, bool thruBuilder = false)
    {
        // Open the Model from stream.
        SolutionModel model = await SolutionSerializers.SlnXml.OpenAsync(slnStream.Name, slnStream.Stream, CancellationToken.None);
        AssertEmptySerializationLog(model);

        if (thruBuilder)
        {
            SolutionModel.Builder newModel = new SolutionModel.Builder(model, stringTable: null);

            // Strip off any comments or whitespace from the original model.
            model = newModel.ToModel(model.SerializerExtension.Serializer.CreateModelExtension());
        }

        // Save the Model back to stream.
        FileContents reserializedSolution = await ModelToLinesAsync(SolutionSerializers.SlnXml, model, slnStream.Name);

        AssertSolutionsAreEqual(slnStream.ToLines(), reserializedSolution);
    }
}
