// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.VisualStudio.SolutionPersistence.Model;
using Microsoft.VisualStudio.SolutionPersistence.Serializer;
using Microsoft.VisualStudio.SolutionPersistence.Serializer.Xml;
using Utilities;
using Xunit;
using Xunit.Sdk;
using static Utilities.SlnTestHelper;

namespace Serialization;

/// <summary>
/// These tests validate SLNX files can be round-tripped through the serializer and model.
/// These remove any user comments and whitespace from the original model.
/// </summary>
public class RoundTripXmlSlnxThruModelCopy
{
    [Fact]
    public Task CommentsAsync()
    {
        return Assert.ThrowsAsync<FailException>(() => TestRoundTripSerializerAsync(SlnAssets.XmlSlnxComments));
    }

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

    [Fact]
    public Task GiantAsync() => TestRoundTripSerializerAsync(SlnAssets.XmlSlnxGiant);

    [Fact]
    [Trait("TestCategory", "FailsInCloudTest")]
    public Task TraditionalAsync() => TestRoundTripSerializerAsync(SlnAssets.XmlSlnxTraditional);

    private static async Task TestRoundTripSerializerAsync(ResourceStream slnStream)
    {
        // Open the Model from stream.
        SolutionModel model = await SolutionSerializers.SlnXml.OpenAsync(slnStream.Stream, CancellationToken.None);
        AssertNotTarnished(model);

        // Make a copy of the model.
        model = new SolutionModel(model)
        {
            // Strip off any comments or whitespace from the original model.
            SerializerExtension = SolutionSerializers.SlnXml.CreateModelExtension(
                new SlnxSerializerSettings()
                {
                    PreserveWhitespace = false,
                }),
        };

        // Save the Model back to stream.
        FileContents reserializedSolution = await ModelToLinesAsync(SolutionSerializers.SlnXml, model);

        AssertSolutionsAreEqual(slnStream.ToLines(), reserializedSolution);
    }
}
