// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Serialization;

/// <summary>
/// These tests validate SLNX files can be round-tripped through the serializer and model.
/// </summary>
[SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1600:Elements should be documented", Justification = "All tests in this class have the same purpose.")]
public class RoundTripXmlSlnx
{
    /// <summary>
    /// Gets the SLNX files to round-trip.
    /// </summary>
    /// <returns>The available SLNX files.</returns>
    public static IEnumerable<ResourceName> XmlSlnxFiles() => SlnAssets.XmlSlnxFiles;

    [Test]
    public Task CommentsAsync() => TestRoundTripSerializerAsync(SlnAssets.XmlSlnxComments);

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
    public Task TraditionalAsync() => TestRoundTripSerializerAsync(SlnAssets.XmlSlnxTraditional);

    [Test]
    public Task MissingConfigurationsAsync() => TestRoundTripSerializerAsync(SlnAssets.XmlSlnxMissingConfigurations);

    [Test]
    public Task VersionAsync() => TestRoundTripSerializerAsync(SlnAssets.XmlSlnxVersion);

    [Test]
    public Task VersionMinAsync() => TestRoundTripSerializerAsync(SlnAssets.XmlSlnxVersionMin);

    [Test]
    public Task ReportProject() => TestRoundTripSerializerAsync(SlnAssets.LoadResource("Report Project.slnx"));

    [Test]
    [MethodDataSource(nameof(XmlSlnxFiles))]
    public Task AllXmlSolutionAsync(ResourceName sampleFile)
    {
        return TestRoundTripSerializerAsync(sampleFile.Load());
    }

    private static async Task TestRoundTripSerializerAsync(ResourceStream slnStream)
    {
        // Open the Model from stream.
        SolutionModel model = await SolutionSerializers.SlnXml.OpenAsync(slnStream.Stream, CancellationToken.None);
        AssertNotTarnished(model);

        // Save the Model back to stream.
        FileContents reserializedSolution = await model.ToLinesAsync(SolutionSerializers.SlnXml);

        AssertSolutionsAreEqual(slnStream.ToLines(), reserializedSolution);
    }
}
