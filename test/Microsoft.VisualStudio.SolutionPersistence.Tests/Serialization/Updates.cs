// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;

namespace Serialization;

public class Updates
{
    [Fact]
    public void ConvertASCIItoUTF8()
    {
        // TODO: Test that SLN converts to UTF-8 automatically from ASCII.
        Assert.True(true);
    }

    // TODO:
    // SLN->SLNX Make sure we strip: ProjectId, SolutionId, ProjectTypeId, DisplayName
    // When RemoveLegacyProperties is on.

    // TODO:
    // Make sure we keep the ProjectId, SolutionId, ProjectTypeId, DisplayName when RemoveLegacyProperties is off.

    // TODO:
    // Test removing preserved order in property tables.

    // TODO:
    // Respect min versioning

    // TODO:
    // Do we keep metadata on projects moved to a new folder

    // TODO:
    // Test renaming a solution folder

    // TODO:
    // Add extra properties in SolutionProperties and ExtensionProperties sections.

    // TODO:
    // Test slnx with 2 identical folder items
}
