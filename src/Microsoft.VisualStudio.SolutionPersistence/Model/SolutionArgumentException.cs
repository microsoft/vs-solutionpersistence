using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.SolutionPersistence.Model;

/// <summary>
/// Enumeration of all different exception types.
/// </summary>
public enum SolutionArgumentExceptionType
{
    CannotMoveFolderToChildFolder,
    DuplicateDefaultProjectType,
    DuplicateExtension,
    DuplicateItemRef,
    DUplicateName,
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
    public SolutionArgumentExceptionType? Type;

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
