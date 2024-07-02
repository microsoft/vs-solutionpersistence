// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.SolutionPersistence.Serializer;

/// <summary>
/// An exception that is thrown when a solution file is not in the expected format.
/// </summary>
[Serializable]
public class InvalidSolutionFormatException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidSolutionFormatException"/> class.
    /// </summary>
    public InvalidSolutionFormatException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidSolutionFormatException"/> class.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public InvalidSolutionFormatException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidSolutionFormatException"/> class.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="inner">The exception that is the cause of the current exception.</param>
    public InvalidSolutionFormatException(string message, Exception inner)
        : base(message, inner)
    {
    }

#if NETFRAMEWORK
    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidSolutionFormatException"/> class.
    /// Used for serialization in .NET Framework.
    /// </summary>
    /// <param name="info">Serialization info.</param>
    /// <param name="context">Contextual info.</param>
    [SuppressMessage("ApiDesign", "RS0016:Add public types and members to the declared API", Justification = "Only in .NET Framework.")]
    protected InvalidSolutionFormatException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context)
        : base(info, context)
    {
    }
#endif
}
