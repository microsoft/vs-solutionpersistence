// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// Instead of creating classes with named properties, use global usings
// to make named tuples available throughout the project.
global using FileContents = (string FullString, System.Collections.Generic.List<string> Lines); // Represents the contents of a file with a version of the file that has all lines concatenated and a list of lines.
global using ResourceStream = (string Name, System.IO.Stream Stream); // Represents a resource loaded from the assembly.
