// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.VisualStudio.SolutionPersistence.Model;
using Microsoft.VisualStudio.SolutionPersistence.Serializer;
using Utilities;
using Xunit;

namespace Serialization;

/// <summary>
/// This test generates files from the .sln tests assets included in the project.
/// It outputs the reset to the temp directory <see cref="OutputDirectory"/>.
/// These can be used to syncronize converted test assets for any changes.
/// </summary>
/// <param name="fixture">Fixture to ensure a temp directory is created for all the tests.</param>
public sealed partial class MakeSlnx(MakeSlnx.MakeSlnxFixture fixture) : IClassFixture<MakeSlnx.MakeSlnxFixture>
{
    public static TheoryData<ResourceName> ClassicSlnFiles =>
        new TheoryData<ResourceName>(SlnAssets.ClassicSlnFiles);

    public static TheoryData<ResourceName> XmlSlnxFiles =>
        new TheoryData<ResourceName>(SlnAssets.XmlSlnxFiles);

    /// <summary>
    /// Converts all the sample SLN files into SLNX and puts them in the temp <see cref="OutputDirectory"/> directory.
    /// </summary>
    /// <param name="slnFileName">The file to convert.</param>
    [Theory]
    [MemberData(nameof(ClassicSlnFiles))]
    [Trait("TestCategory", "FailsInCloudTest")]
    public async Task ConvertSlnToSlnxAsync(ResourceName slnFileName)
    {
        ResourceStream slnFile = slnFileName.Load();
        int sampleFileSize = checked((int)slnFile.Stream.Length);
        string slnToSlnxFile = Path.ChangeExtension(Path.Join(fixture.SlnToSlnxDirectory, slnFile.Name), SolutionSerializers.SlnXml.DefaultFileExtension);
        string slnViaSlnxFile = Path.Join(fixture.SlnViaSlnxDirectory, slnFile.Name);

        SolutionModel model = await SolutionSerializers.SlnFileV12.OpenAsync(slnFile.Stream, CancellationToken.None);

        // Original sln converted to slnx
        await SolutionSerializers.SlnXml.SaveAsync(slnToSlnxFile, model, CancellationToken.None);

        // Original sln converted back to sln via slnx file
        (SolutionModel slnxModel, _) = await SlnTestHelper.ThruSlnxStreamAsync(model, sampleFileSize * 10);
        await SolutionSerializers.SlnFileV12.SaveAsync(slnViaSlnxFile, slnxModel, CancellationToken.None);

        // Make it easy to update open and update samples.
        File.Move(slnToSlnxFile, slnToSlnxFile + ".xml");
        File.Move(slnViaSlnxFile, slnViaSlnxFile + ".txt");
    }

    [Theory]
    [MemberData(nameof(XmlSlnxFiles))]
    [Trait("TestCategory", "FailsInCloudTest")]
    public async Task ConvertSlnxToSlnAsync(ResourceName slnxFileName)
    {
        ResourceStream slnxFile = slnxFileName.Load();

        string slnxToSlnFile = Path.ChangeExtension(Path.Join(fixture.SlnxToSlnDirectory, slnxFile.Name), SolutionSerializers.SlnFileV12.DefaultFileExtension);

        SolutionModel model = await SolutionSerializers.SlnXml.OpenAsync(slnxFile.Stream, CancellationToken.None);

        // Convert slnx to sln
        await SolutionSerializers.SlnFileV12.SaveAsync(slnxToSlnFile, model, CancellationToken.None);

        // Make it easy to update open and update samples.
        File.Move(slnxToSlnFile, slnxToSlnFile + ".txt");
    }
}
