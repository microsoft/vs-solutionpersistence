// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Serialization;

/// <summary>
/// Tests for solution filters.
/// </summary>
public class SolutionFilters
{
    /// <summary>
    /// Tests opening and parsing slnf files.
    /// </summary>
    [Fact]
    public async Task ReadSolutionFilterProjects()
    {
        string slnfFilePath = SlnAssets.SlnfExample.SaveResourceToTempFile();
        string slnOriginalFilePath = SlnAssets.SlnfOriginal.SaveResourceToTempFile().Replace("\\", "\\\\");

        // Edit slnf file to point to the original sln file.
        string slnfContent = File.ReadAllText(slnfFilePath);
        slnfContent = slnfContent.Replace("OriginalSolution.slnx", slnOriginalFilePath);
        File.WriteAllText(slnfFilePath, slnfContent);

        // Get ResourceStream for slnf file.
        using Stream slnfStream = File.OpenRead(slnfFilePath);

        SolutionModel solution = await SolutionSerializers.SlnfJson.OpenAsync(slnfStream, CancellationToken.None);
        if (solution.SolutionProjects.Count != 3)
        {
            throw new InvalidOperationException("Expected 3 projects in the solution.");
        }
    }
}
