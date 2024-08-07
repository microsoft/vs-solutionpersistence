﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

[SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1649:File name should match first type name", Justification = "Test helper.")]
public record struct FileContents(string FullString, List<string> Lines); // Represents the contents of a file with a version of the file that has all lines concatenated and a list of lines.

[SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1649:File name should match first type name", Justification = "Test helper.")]
[SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:File may only contain a single type", Justification = "Test helper.")]
public record struct ResourceName(string Name, string FullResourceId); // Represents a resource name and an identifier.

[SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1649:File name should match first type name", Justification = "Test helper.")]
[SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:File may only contain a single type", Justification = "Test helper.")]
public record struct ResourceStream(string Name, Stream Stream); // Represents a resource loaded from the assembly.
