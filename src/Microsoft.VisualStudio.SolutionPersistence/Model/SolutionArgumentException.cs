// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.VisualStudio.SolutionPersistence.Model;

public class SolutionArgumentException : ArgumentException
{
    public readonly SolutionErrorType Type;

    public SolutionArgumentException(string? message, SolutionErrorType type)
        : base(message)
    {
        this.Type = type;
    }

    public SolutionArgumentException(string? message, Exception? innerException, SolutionErrorType type)
        : base(message, innerException)
    {
        this.Type = type;
    }

    public SolutionArgumentException(string? message, string? paramName, SolutionErrorType type)
        : base(message, paramName)
    {
        this.Type = type;
    }

    public SolutionArgumentException(string? message, string? paramName, Exception? innerException, SolutionErrorType type)
        : base(message, paramName, innerException)
    {
        this.Type = type;
    }
}
