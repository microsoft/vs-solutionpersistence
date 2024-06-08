// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.SolutionPersistence.Serializer;

[Serializable]
public class InvalidSolutionFormatException : Exception
{
    public InvalidSolutionFormatException()
    {
    }

    public InvalidSolutionFormatException(string message)
        : base(message)
    {
    }

    public InvalidSolutionFormatException(string message, Exception inner)
        : base(message, inner)
    {
    }

#if NETFRAMEWORK
    [SuppressMessage("ApiDesign", "RS0016:Add public types and members to the declared API", Justification = "Only in .NET Framework.")]
    protected InvalidSolutionFormatException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context)
        : base(info, context)
    {
    }
#endif
}
