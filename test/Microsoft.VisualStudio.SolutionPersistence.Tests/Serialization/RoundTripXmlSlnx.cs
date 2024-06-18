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
    public static TheoryData<ResourceName> XmlSlnxFiles =>
        new TheoryData<ResourceName>(SlnAssets.XmlSlnxFiles);

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
    public Task AllXmlSolutionAsync(ResourceName sampleFile)
    {
        return TestRoundTripSerializerAsync(sampleFile.Load());
    }

    #region slnx -> sln -> slnx round trip

    [Fact]
    public Task CommentsThruModelCopyAsync()
    {
        return Assert.ThrowsAsync<FailException>(() => TestRoundTripSerializerAsync(SlnAssets.XmlSlnxComments, thruModelCopy: true));
    }

    [Fact]
    public Task BlankThruModelCopyAsync() => TestRoundTripSerializerAsync(SlnAssets.XmlSlnxBlank, thruModelCopy: true);

    [Fact]
    public Task CpsThruModelCopyAsync() => TestRoundTripSerializerAsync(SlnAssets.XmlSlnxCps, thruModelCopy: true);

    [Fact]
    public Task EverythingThruModelCopyAsync() => TestRoundTripSerializerAsync(SlnAssets.XmlSlnxEverything, thruModelCopy: true);

    [Fact]
    public Task OrchardCoreThruModelCopyAsync() => TestRoundTripSerializerAsync(SlnAssets.XmlSlnxOrchardCore, thruModelCopy: true);

    [Fact]
    public Task SingleNativeProjectThruModelCopyAsync() => TestRoundTripSerializerAsync(SlnAssets.XmlSlnxSingleNativeProject, thruModelCopy: true);

    [Fact]
    public Task BuiltInProjectTypesThruModelCopyAsync() => TestRoundTripSerializerAsync(SlnAssets.XmlBuiltInProjectTypes, thruModelCopy: true);

    #endregion

    private static async Task TestRoundTripSerializerAsync(ResourceStream slnStream, bool thruModelCopy = false)
    {
        // Open the Model from stream.
        SolutionModel model = await SolutionSerializers.SlnXml.OpenAsync(slnStream.Name, slnStream.Stream, CancellationToken.None);
        AssertEmptySerializationLog(model);

        if (thruModelCopy)
        {
            model = new SolutionModel(model)
            {
                // Strip off any comments or whitespace from the original model.
                SerializerExtension = SolutionSerializers.SlnXml.CreateModelExtension(),
            };
        }

        // Save the Model back to stream.
        FileContents reserializedSolution = await ModelToLinesAsync(SolutionSerializers.SlnXml, model, slnStream.Name);

        AssertSolutionsAreEqual(slnStream.ToLines(), reserializedSolution);
    }
}
