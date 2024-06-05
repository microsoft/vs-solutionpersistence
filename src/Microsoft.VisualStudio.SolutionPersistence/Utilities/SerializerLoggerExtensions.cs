// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Xml;

namespace Microsoft.VisualStudio.SolutionPersistence.Utilities;

internal static class SerializerLoggerExtensions
{
    public static void LogError(this ISerializerLogger logger, string message, XmlElement? location = null)
    {
        logger.Log(message, MessageLevel.Error, location);
    }

    public static void LogWarning(this ISerializerLogger logger, string message, XmlElement? location = null)
    {
        logger.Log(message, MessageLevel.Warning, location);
    }

    public static void LogMessage(this ISerializerLogger logger, string message, XmlElement? location = null)
    {
        logger.Log(message, MessageLevel.Message, location);
    }
}
