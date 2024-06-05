// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq;
using System.Xml;
using Microsoft.VisualStudio.SolutionPersistence.Model;
using Microsoft.VisualStudio.SolutionPersistence.Serializer.Xml.XmlDecorators;
using Microsoft.VisualStudio.SolutionPersistence.Utilities;

namespace Microsoft.VisualStudio.SolutionPersistence.Serializer.Xml;

internal sealed partial class SlnXmlSerializer
{
    private sealed partial class Reader
    {
        private readonly string? fullPath;
        private readonly XmlDocument xmlDocument;

        public Reader(string? fullPath, Stream readerStream)
        {
            this.fullPath = fullPath;

            // We ideally want to preserver whitespace, but if this is on
            // we need to manually handle preserving all indenting and new lines
            // when elements are added or removed.
            this.xmlDocument = new XmlDocument() { PreserveWhitespace = true };
            this.xmlDocument.Load(readerStream);
        }

        public SolutionModel Parse(ISolutionSerializer serializer)
        {
            SlnxFile slnxFile = new SlnxFile(serializer, this.xmlDocument, new SlnxSerializerSettings(), null, this.fullPath);
            SerializerLogger logger = slnxFile.Logger;
            (string message, MessageLevel level, XmlElement? location) = logger.Messages.FirstOrDefault(x => x.Level == MessageLevel.Error);
            if (level == MessageLevel.Error)
            {
                throw new InvalidSolutionFormatException(message);
            }

            return slnxFile.ToModel();
        }
    }
}
