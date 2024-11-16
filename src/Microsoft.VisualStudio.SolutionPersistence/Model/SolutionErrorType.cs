// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.SolutionPersistence.Model;

/// <summary>
/// Reasons the SolutionArgumentException was raised.
/// </summary>
public enum SolutionErrorType
{
    Undefined,
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
    InvalidProjectTypeReference,
    InvalidVersion,
    MissingProjectValue,
    NotSolution,
    UnsupportedVersion,
    InvalidXmlDecoratorElementName
}
