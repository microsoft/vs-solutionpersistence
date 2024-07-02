﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.VisualStudio.SolutionPersistence.Model;
using Microsoft.VisualStudio.SolutionPersistence.Serializer;
using Utilities;
using Xunit;
using static Utilities.SlnTestHelper;

namespace Serialization;

/// <summary>
/// These tests validate SLN files can be round-tripped through the SLNX format.
/// </summary>
public class RoundTripClassicSlnThruSlnxStream
{
    [Fact]
    [Trait("TestCategory", "FailsInCloudTest")]
    public Task BlankThruSlnxStreamAsync() => TestRoundTripSerializerAsync(SlnAssets.ClassicSlnBlank, SlnAssets.XmlSlnxBlank);

    [Fact]
    public Task CpsThruSlnxStreamAsync() => TestRoundTripSerializerAsync(SlnAssets.ClassicSlnCps, SlnAssets.XmlSlnxCps);

    [Fact]
    public Task EverythingThruSlnxStreamAsync() => TestRoundTripSerializerAsync(SlnAssets.ClassicSlnEverything, SlnAssets.XmlSlnxEverything);

    [Fact]
    [Trait("TestCategory", "FailsInCloudTest")]
    public Task OrchardCoreThruSlnxStreamAsync() => TestRoundTripSerializerAsync(SlnAssets.ClassicSlnOrchardCore, SlnAssets.XmlSlnxOrchardCore);

    [Fact]
    [Trait("TestCategory", "FailsInCloudTest")]
    public Task SingleNativeProjectThruSlnxStreamAsync() => TestRoundTripSerializerAsync(SlnAssets.ClassicSlnSingleNativeProject, SlnAssets.XmlSlnxSingleNativeProject);

    [Fact]
    public Task ManyThruSlnxStreamAsync() => TestRoundTripSerializerAsync(SlnAssets.ClassicSlnMany, SlnAssets.XmlSlnxMany);

    [Fact]
    public Task GiantThruSlnxStreamAsync() => TestRoundTripSerializerAsync(SlnAssets.ClassicSlnGiant, SlnAssets.XmlSlnxGiant);

    [Fact]
    [Trait("TestCategory", "FailsInCloudTest")]
    public Task TraditionalThruSlnxStreamAsync() => TestRoundTripSerializerAsync(SlnAssets.ClassicSlnTraditional, SlnAssets.XmlSlnxTraditional);

    /// <summary>
    /// Round trip a .SLN file through the slnx serializer.
    /// </summary>
    /// <param name="slnStream">The .SLN to test.</param>
    /// <param name="viaSlnxStream">When set, saves the .SLN as a .SLNX and reload. This provides the expected .SLNX file.</param>
    /// <returns>Task to track the asynchronous call status.</returns>
    private static async Task TestRoundTripSerializerAsync(
        ResourceStream slnStream,
        ResourceStream viaSlnxStream)
    {
        FileContents originalSolution = slnStream.ToLines();

        // Open the Model from stream.
        SolutionModel model = await SolutionSerializers.SlnFileV12.OpenAsync(slnStream.Stream, CancellationToken.None);
        AssertNotTarnished(model);

        (model, FileContents slnxContents) = await ThruSlnxStreamAsync(model, originalSolution.FullString.Length * 10);

        AssertSolutionsAreEqual(viaSlnxStream.ToLines(), slnxContents);

        // Save the Model back to stream.
        FileContents reserializedSolution = await ModelToLinesAsync(SolutionSerializers.SlnFileV12, model);

        AssertSolutionsAreEqual(originalSolution, reserializedSolution);
    }
}