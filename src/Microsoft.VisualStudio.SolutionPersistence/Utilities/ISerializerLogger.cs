// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Xml;

namespace Microsoft.VisualStudio.SolutionPersistence.Utilities;

/// <summary>
/// Provides a way to log messages during serialization.
/// </summary>
internal interface ISerializerLogger
{
    /// <summary>
    /// Logs a message during serialization or model building.
    /// </summary>
    /// <param name="message">The message.</param>
    /// <param name="level">The severity of the message.</param>
    /// <param name="location">[Optional] The element where the message originated from.</param>
    void Log(string message, MessageLevel level, XmlElement? location);
}
