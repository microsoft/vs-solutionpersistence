// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq;
using System.Text.Json;
using Microsoft.VisualStudio.SolutionPersistence.Model;

namespace Microsoft.VisualStudio.SolutionPersistence.Serializer.SlnfJson;

internal sealed partial class SlnfJsonSerializer
{
    private sealed partial class Reader
    {
#pragma warning disable IDE0052 // Remove unread private members
        private readonly string? fullPath;
#pragma warning restore IDE0052 // Remove unread private members
        private readonly JsonDocument jsonDocument;

        internal Reader(string? fullPath, Stream readerStream)
        {
            this.fullPath = fullPath;
            this.jsonDocument = JsonDocument.Parse(readerStream);
        }

        internal SolutionModel Parse()
        {
            SolutionModel filteredSolution = new SolutionModel();
            filteredSolution.SerializerExtension = new SlnfJsonModelExtension(SolutionSerializers.SlnfJson, new SlnfJsonSerializerSettings(), this.fullPath);

            JsonElement root = this.jsonDocument.RootElement;
            JsonElement solution = root.GetProperty("solution");
            string originalSolutionPath = solution.GetProperty("path").GetString()!;
            string[] projectPaths = solution.GetProperty("projects").EnumerateArray().Select(projectPath => projectPath.GetString()!).ToArray();

            ISolutionSerializer originalSolutionSerializer = SolutionSerializers.GetSerializerByMoniker(originalSolutionPath)!;
            SolutionModel originalSolution = originalSolutionSerializer.OpenAsync(originalSolutionPath, CancellationToken.None).Result;

            List<SolutionProjectModel> projects = originalSolution.SolutionProjects.Where(project => projectPaths.Contains(project.FilePath)).ToList();

            projects.ForEach(project =>
                filteredSolution.AddProject(
                    project.FilePath,
                    project.Type,
                    project.Parent is not null ? filteredSolution.AddFolder(project.Parent.Path) : null));

            return filteredSolution;
        }
    }
}
