// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.VisualStudio.SolutionPersistence.Model;
using Microsoft.VisualStudio.SolutionPersistence.Serializer;
using Utilities;
using Xunit;

namespace Serialization;

#pragma warning disable CS9113 // Parameter is unread.
public partial class MakeSlnx(MakeSlnx.MakeSlnxFixture fixture) : IClassFixture<MakeSlnx.MakeSlnxFixture>
#pragma warning restore CS9113 // Parameter is unread.
{
    public static TheoryData<ResourceName> ClassicSlnFiles =>
        new TheoryData<ResourceName>(SlnAssets.ClassicSlnFiles);

    /// <summary>
    /// Converts all the sample SLN files into SLNX and puts them in the temp "OutputSln" directory.
    /// </summary>
    /// <param name="sampleFileName">The file to convert.</param>
    [Theory]
    [MemberData(nameof(ClassicSlnFiles))]
    [Trait("TestCategory", "FailsInCloudTest")]
    public async Task ConvertSlnToSlnxAsync(ResourceName sampleFileName)
    {
        ResourceStream sampleFile = sampleFileName.Load();
        int sampleFileSize = checked((int)sampleFile.Stream.Length);
        string outputDirectory = Path.Combine(Path.GetTempPath(), "OutputSln");
        string convertedSlnx = Path.ChangeExtension(Path.Join(outputDirectory, "slnx", sampleFile.Name), SolutionSerializers.SlnXml.DefaultFileExtension);
        string sln = Path.Join(outputDirectory, "sln", sampleFile.Name);
        string slnThruSlnxStream = Path.Join(outputDirectory, "slnThruSlnxStream", sampleFile.Name);

        SolutionModel model = await SolutionSerializers.SlnFileV12.OpenAsync(sampleFile.Name, sampleFile.Stream, CancellationToken.None);

        // Original sln converted to slnx
        await SolutionSerializers.SlnXml.SaveAsync(convertedSlnx, model, CancellationToken.None);

        // Original sln re-saved
        await SolutionSerializers.SlnFileV12.SaveAsync(sln, model, CancellationToken.None);

        // Original sln converted back to sln via slnx file
        (SolutionModel slnxModel, _) = await SlnTestHelper.ThruSlnStreamAsync(model, sampleFile.Name, sampleFileSize * 10);
        await SolutionSerializers.SlnFileV12.SaveAsync(slnThruSlnxStream, slnxModel, CancellationToken.None);

        // Make it easy to update open and update samples.
        File.Move(convertedSlnx, convertedSlnx + ".xml");
        File.Move(sln, sln + ".txt");
        File.Move(slnThruSlnxStream, slnThruSlnxStream + ".txt");
    }
}
