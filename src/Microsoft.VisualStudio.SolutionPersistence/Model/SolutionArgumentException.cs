// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.VisualStudio.SolutionPersistence.Model;

/// <summary>
/// Reasons the SolutionArgumentException was raised.
/// </summary>
public enum SolutionArgumentExceptionType
{
    CannotMoveFolderToChildFolder,
    DuplicateDefaultProjectType,
    DuplicateExtension,
    DuplicateItemRef,
    DuplicateName,
    DuplicateProjectName,
    DuplicateProjectPath,
    DuplicateProjectTypeId,
    InvalidConfiguration,
    InvalidEncoding,
    InvalidFolderPath,
    InvalidFolderReference,
    InvalidItemRef,
    InvalidLoop,
    InvalidModelItem,
    InvalidName,
    InvalidProjectReference,
    InvalidProjectType,
    InvalidProjectTypeReference,
    InvalidScope,
    InvalidVersion,
    MissingDisplayName,
    MissingPath,
    MissingProjectId,
    MissingProjectValue,
    MissingSectionName,
    NotSolution,
    SyntaxError,
    UnsupportedVersion,
    InvalidXmlDecoratorElementName
}

public class SolutionArgumentException : ArgumentException
{
    public SolutionArgumentExceptionType? Type { get; private set; }

    public SolutionArgumentException(string? message, SolutionArgumentExceptionType? type)
        : base(message)
    {
        this.Type = type;
    }

    public SolutionArgumentException(string? message, Exception? innerException, SolutionArgumentExceptionType? type)
        : base(message, innerException)
    {
        this.Type = type;
    }

    public SolutionArgumentException(string? message, string? paramName, SolutionArgumentExceptionType? type)
        : base(message, paramName)
    {
        this.Type = type;
    }

    public SolutionArgumentException(string? message, string? paramName, Exception? innerException, SolutionArgumentExceptionType? type)
        : base(message, paramName, innerException)
    {
        this.Type = type;
    }
}
