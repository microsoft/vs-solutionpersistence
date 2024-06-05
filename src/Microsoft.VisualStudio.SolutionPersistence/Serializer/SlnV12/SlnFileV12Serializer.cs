// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.VisualStudio.SolutionPersistence.Model;
using Microsoft.VisualStudio.SolutionPersistence.Utilities;

namespace Microsoft.VisualStudio.SolutionPersistence.Serializer.SlnV12;

/// <summary>
/// Serializer for classic .sln solution files (version 12).
/// </summary>
internal sealed partial class SlnFileV12Serializer : SingleFileSerializerBase<SlnV12SerializerSettings>
{
    private static string Extension => ".sln";

    private static string SerializerName => "SlnV12";

    internal enum ParseError
    {
        NotASln12File,
        BadSln12File,
    }

    [Obsolete("Use Instance")]
    public SlnFileV12Serializer()
    {
    }

    public static SlnFileV12Serializer Instance => Singleton<SlnFileV12Serializer>.Instance;

    public override ISerializerModelExtension CreateModelExtension()
    {
        return new SlnV12ModelExtension(this, new SlnV12SerializerSettings() { Encoding = Encoding.ASCII });
    }

    public override ISerializerModelExtension CreateModelExtension(SlnV12SerializerSettings settings)
    {
        if (settings.Encoding is not null && settings.Encoding.CodePage != Encoding.ASCII.CodePage &&
            settings.Encoding.CodePage != Encoding.UTF8.CodePage &&
            settings.Encoding.CodePage != Encoding.Unicode.CodePage)
        {
            throw new ArgumentException("Only ASCII, UTF-8, and Unicode encodings are supported.", nameof(settings));
        }

        return new SlnV12ModelExtension(this, settings);
    }

    private protected override string FileExtension => Extension;

    public override string Name => SerializerName;

    private protected override async Task<SolutionModel> ReadModelAsync(string? fullPath, Stream reader, CancellationToken cancellationToken)
    {
        // NOTE: Encoding.Default is the Windows ANSI code page in .NET Framework, but UTF-8 in .NET Core.
        using StreamReader streamReader = new StreamReader(reader, Encoding.Default, detectEncodingFromByteOrderMarks: true);
        return await new Reader(streamReader, fullPath).ParseAsync(this, fullPath, cancellationToken);
    }

    private protected override async Task WriteModelAsync(string? fullPath, SolutionModel model, Stream writerStream, CancellationToken cancellationToken)
    {
        try
        {
            await SlnFileV12Writer.SaveAsync(model, writerStream);
        }
        catch (EncoderFallbackException)
        {
            // Change the model to save it in UTF-8 and retry.
            SolutionModel.Builder utf8ModelBuilder = new SolutionModel.Builder(model, stringTable: null);
            SolutionModel utf8Model = utf8ModelBuilder.ToModel(new SlnV12ModelExtension(this, new SlnV12SerializerSettings() { Encoding = Encoding.UTF8 }, fullPath));

            await SlnFileV12Writer.SaveAsync(utf8Model, writerStream);
        }
    }
}
