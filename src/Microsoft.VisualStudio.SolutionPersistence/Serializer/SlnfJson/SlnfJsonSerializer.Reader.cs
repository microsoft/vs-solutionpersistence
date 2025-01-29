// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.VisualStudio.SolutionPersistence.Model;

namespace Microsoft.VisualStudio.SolutionPersistence.Serializer.SlnfJson;

internal sealed partial class SlnfJsonSerializer
{
    private sealed partial class Reader
    {
#pragma warning disable IDE0052 // Remove unread private members
        private readonly string? fullPath;
#pragma warning restore IDE0052 // Remove unread private members
        private readonly JsonNode jsonNode;

        internal Reader(string? fullPath, Stream readerStream)
        {
            this.fullPath = fullPath;
            try
            {
                // Read json document from stream
                _ = readerStream.Seek(0, SeekOrigin.Begin);
                this.jsonNode = JsonNode.Parse(readerStream) ?? throw new SolutionException(Errors.NotSolution, SolutionErrorType.NotSolution);
            }
            catch
            {
                throw new SolutionException(Errors.NotSolution, SolutionErrorType.NotSolution);
            }
        }

        internal SolutionModel Parse()
        {
            string originalSolutionPath = this.jsonNode["solution"]?["path"]?.GetValue<string>() ?? string.Empty;
            string[] projectPaths = this.jsonNode["solution"]?["projects"]?.AsArray()?.GetValues<string>()?.ToArray<string>() ?? [];

            // Create filtered solution
            SolutionModel filteredSolution = new SolutionModel();
            filteredSolution.FilteredOriginalSolutionFilePath = originalSolutionPath;
            filteredSolution.SerializerExtension = new SlnfJsonModelExtension(SolutionSerializers.SlnfJson, new SlnfJsonSerializerSettings(), this.fullPath);

            // Get original solution
            ISolutionSerializer originalSolutionSerializer = SolutionSerializers.GetSerializerByMoniker(originalSolutionPath)!;
            SolutionModel originalSolution = originalSolutionSerializer.OpenAsync(originalSolutionPath, CancellationToken.None).Result;

            // Filter projects
            IEnumerable<SolutionProjectModel> filteredProjects = projectPaths
                .Select(path => path.Replace('\\', Path.DirectorySeparatorChar))
                .Select(path => originalSolution.FindProject(path) ??
                    throw new SolutionException(string.Format(Errors.InvalidProjectReference_Args1, path), SolutionErrorType.InvalidProjectReference));

            foreach (SolutionProjectModel project in filteredProjects)
            {
                _ = filteredSolution.AddProject(project.FilePath, project.Type, project.Parent is not null ? filteredSolution.AddFolder(project.Parent.Path) : null);
            }

            return filteredSolution;
        }
    }
}
