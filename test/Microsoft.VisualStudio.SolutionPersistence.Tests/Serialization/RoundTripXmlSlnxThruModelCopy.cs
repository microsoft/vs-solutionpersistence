// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.VisualStudio.SolutionPersistence.Serializer.Xml;
using Xunit.Sdk;

namespace Serialization;

/// <summary>
/// These tests validate SLNX files can be round-tripped through the serializer and model.
/// These remove any user comments and whitespace from the original model.
/// </summary>
[SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1600:Elements should be documented", Justification = "All tests in this class have the same purpose.")]
public class RoundTripXmlSlnxThruModelCopy
{
    [Test]
    public Task CommentsAsync()
    {
        return Assert.ThrowsAsync<FailException>(() => TestRoundTripSerializerAsync(SlnAssets.XmlSlnxComments));
    }

    [Test]
    public Task LegacyValuesAsync() => TestRoundTripSerializerAsync(SlnAssets.XmlSlnxLegacyValuesNoObsolete);

    [Test]
    public Task BlankAsync() => TestRoundTripSerializerAsync(SlnAssets.XmlSlnxBlank);

    [Test]
    public Task CpsAsync() => TestRoundTripSerializerAsync(SlnAssets.XmlSlnxCps);

    [Test]
    public Task EverythingAsync() => TestRoundTripSerializerAsync(SlnAssets.XmlSlnxEverything);

    [Test]
    public Task OrchardCoreAsync() => TestRoundTripSerializerAsync(SlnAssets.XmlSlnxOrchardCore);

    [Test]
    public Task SingleNativeProjectAsync() => TestRoundTripSerializerAsync(SlnAssets.XmlSlnxSingleNativeProject);

    [Test]
    public Task BuiltInProjectTypesAsync() => TestRoundTripSerializerAsync(SlnAssets.XmlBuiltInProjectTypes);

    [Test]
    public Task GiantAsync() => TestRoundTripSerializerAsync(SlnAssets.XmlSlnxGiant);

    [Test]
    public Task MissingConfigurationsAsync() => TestRoundTripSerializerAsync(SlnAssets.XmlSlnxMissingConfigurations);

    [Test]
    public Task TraditionalAsync() => TestRoundTripSerializerAsync(SlnAssets.XmlSlnxTraditional);

    [Test]
    public Task ReportProjectAsync() => TestRoundTripSerializerAsync(SlnAssets.LoadResource("Report Project.slnx"));

    private static async Task TestRoundTripSerializerAsync(ResourceStream slnStream)
    {
        // Open the Model from stream.
        SolutionModel model = await SolutionSerializers.SlnXml.OpenAsync(slnStream.Stream, CancellationToken.None);
        AssertNotTarnished(model);

        Assert.True(model.TryGetSettings(out SlnxSerializerSettings originalSettings));

        // Make a copy of the model.
        model = new SolutionModel(model)
        {
            // Strip off any comments or whitespace from the original model, but keep the indentation the same.
            SerializerExtension = SolutionSerializers.SlnXml.CreateModelExtension(
                new SlnxSerializerSettings(originalSettings)
                {
                    PreserveWhitespace = false,
                }),
        };

        // Save the Model back to stream.
        FileContents reserializedSolution = await model.ToLinesAsync(SolutionSerializers.SlnXml);

        AssertSolutionsAreEqual(slnStream.ToLines(), reserializedSolution);
    }
}
