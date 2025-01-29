// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.VisualStudio.SolutionPersistence.Model;

namespace Microsoft.VisualStudio.SolutionPersistence.Serializer.SlnfJson;

internal partial class SlnfJsonSerializer
{
    private static class Writer
    {
        internal static async Task SaveAsync(
            string? fullPath,
            SolutionModel model,
            Stream streamWriter)
        {
            var slnfDictionary = new Dictionary<string, object>
            {
                {
                    "solution", new Dictionary<string, object>
                    {
                        { "path", model.FilteredOriginalSolutionFilePath! },
                        { "projects", model.SolutionProjects.Select(p => p.FilePath).ToList() },
                    }
                },
            };

            string slnfJsonString = JsonSerializer.Serialize(slnfDictionary, new JsonSerializerOptions { WriteIndented = true });

            byte[] slnfJsonBytes = Encoding.UTF8.GetBytes(slnfJsonString);
            await streamWriter.WriteAsync(slnfJsonBytes, 0, slnfJsonBytes.Length).ConfigureAwait(false);
        }
    }
}
