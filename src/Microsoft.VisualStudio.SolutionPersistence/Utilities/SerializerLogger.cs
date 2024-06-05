// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Xml;

namespace Microsoft.VisualStudio.SolutionPersistence.Utilities;

internal enum MessageLevel
{
    Message,
    Warning,
    Error,
}

// Basic implementation of logger.
// CONSIDER: Make this more effecient.
internal sealed class SerializerLogger : ISerializerLogger
{
    private readonly List<(string Message, MessageLevel Level, XmlElement? Location)> messages = [];

    public void Log(string message, MessageLevel level, XmlElement? location)
    {
        this.messages.Add((message, level, location));
    }

    public List<(string Message, MessageLevel Level, XmlElement? Location)> Messages => this.messages;

    public override string ToString()
    {
        StringBuilder builder = new();
        foreach ((string message, MessageLevel level, XmlElement? location) in this.messages)
        {
            _ = builder.Append(level).Append($": ").Append(message);
            if (location is not null)
            {
                _ = builder.Append(" (Location: ").Append(location.OuterXml).Append(')');
            }

            _ = builder.AppendLine();
        }

        return builder.ToString();
    }
}
