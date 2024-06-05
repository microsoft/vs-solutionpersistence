// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.VisualStudio.SolutionPersistence.Model;
using Microsoft.VisualStudio.SolutionPersistence.Utilities;

namespace Microsoft.VisualStudio.SolutionPersistence.Serializer.Xml;

internal sealed partial class SlnXmlSerializer : SingleFileSerializerBase<SlnxSerializerSettings>
{
    private const string Extension = ".slnx";

    private const string SerializerName = "Slnx";

    internal enum ParseError
    {
        NoSolutionElement,
        BadXMLorBug,
        BadSlnFile,
    }

    [Obsolete("Use Instance")]
    public SlnXmlSerializer()
    {
    }

    public static SlnXmlSerializer Instance => Singleton<SlnXmlSerializer>.Instance;

    private protected override string FileExtension => Extension;

    public override string Name => SerializerName;

    public override ISerializerModelExtension CreateModelExtension()
    {
        return this.CreateModelExtension(new SlnxSerializerSettings()
        {
            // For new documents want to do standard indentation.
            PreserveWhitespace = false,
            IndentChars = "  ",
            NewLine = Environment.NewLine,
        });
    }

    public override ISerializerModelExtension CreateModelExtension(SlnxSerializerSettings settings)
    {
        return new SlnXmlModelExtension(this, settings);
    }

    private protected override Task<SolutionModel> ReadModelAsync(string? fullPath, Stream reader, CancellationToken cancellationToken)
    {
        Reader parser = new Reader(fullPath, reader);
        SolutionModel model = parser.Parse(this);
        return Task.FromResult(model);
    }

    private protected override Task WriteModelAsync(string? fullPath, SolutionModel model, Stream writerStream, CancellationToken cancellationToken)
    {
        return Writer.SaveAsync(fullPath, model, writerStream);
    }
}
