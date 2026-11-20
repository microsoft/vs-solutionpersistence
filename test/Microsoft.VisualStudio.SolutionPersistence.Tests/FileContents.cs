// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/// <summary>
/// Represents the contents of a file with a version of the file that has all lines concatenated and a list of lines.
/// </summary>
/// <param name="FullString">The complete file contents.</param>
/// <param name="Lines">The individual lines.</param>
public record struct FileContents(string FullString, List<string> Lines);
