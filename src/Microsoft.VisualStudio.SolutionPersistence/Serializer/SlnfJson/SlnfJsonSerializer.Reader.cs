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
            try
            {
                // Read json document from stream
                _ = readerStream.Seek(0, SeekOrigin.Begin);
                this.jsonDocument = JsonDocument.Parse(readerStream);
            }
            catch
            {
                throw new SolutionException(Errors.NotSolution, SolutionErrorType.NotSolution);
            }
        }

        internal SolutionModel Parse()
        {
            SolutionModel filteredSolution = new SolutionModel();
            filteredSolution.SerializerExtension = new SlnfJsonModelExtension(SolutionSerializers.SlnfJson, new SlnfJsonSerializerSettings(), this.fullPath);

            JsonElement root = this.jsonDocument.RootElement;
            JsonElement solution = root.GetProperty("solution");
            string originalSolutionPath = solution.GetProperty("path").GetString()!;
            List<string> projectPaths = solution.GetProperty("projects").EnumerateArray().Select(projectPath => projectPath.GetString()!).ToList();

            ISolutionSerializer originalSolutionSerializer = SolutionSerializers.GetSerializerByMoniker(originalSolutionPath)!;
            SolutionModel originalSolution = originalSolutionSerializer.OpenAsync(originalSolutionPath, CancellationToken.None).Result;

            projectPaths.ForEach(projectPath =>
            {
                SolutionProjectModel? project = originalSolution.FindProject(projectPath);
                if (project is not null)
                {
                    _ = filteredSolution.AddProject(
                    project.FilePath,
                    project.Type,
                    project.Parent is not null ? filteredSolution.AddFolder(project.Parent.Path) : null);
                }
                else
                {
                    throw new SolutionException(string.Format(Errors.InvalidProjectReference_Args1, projectPath), SolutionErrorType.InvalidProjectReference);
                }
            });

            return filteredSolution;
        }
    }
}
