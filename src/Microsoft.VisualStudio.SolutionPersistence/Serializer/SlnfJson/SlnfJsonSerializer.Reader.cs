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
            SolutionModel solution = new SolutionModel();
            solution.SerializerExtension = new SlnfJsonModelExtension(SolutionSerializers.SlnfJson, new SlnfJsonSerializerSettings(), this.fullPath);
            JsonElement root = this.jsonDocument.RootElement;
            root.GetProperty("solution").GetProperty("projects").EnumerateArray().ToList().ForEach(projectPath =>
            {
                _ = solution.AddProject(projectPath.GetString()!);
            });
            return solution;
        }
    }
}
