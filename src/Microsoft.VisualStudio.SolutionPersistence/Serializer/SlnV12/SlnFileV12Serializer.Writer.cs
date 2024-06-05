﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.VisualStudio.SolutionPersistence.Model;

namespace Microsoft.VisualStudio.SolutionPersistence.Serializer.SlnV12;

internal partial class SlnFileV12Serializer
{
    /// <summary>
    /// Produces a Format 12.00 solution file on disk from a <see cref=" SolutionModel"/>.
    /// </summary>
    private readonly struct SlnFileV12Writer(SolutionModel model, TextWriter writer)
    {
        public static async Task SaveAsync(
            SolutionModel model,
            Stream streamWriter)
        {
            SlnV12ModelExtension? modelExtension = model.SerializerExtension as SlnV12ModelExtension;

            Encoding? formatEncoding = modelExtension?.Settings.Encoding;

            // Only support unicode based encodings. Note ASCII is a subset of UTF8, if ASCII fails switch to UTF8 with BOM.
            // This should hopefully stop propagating solutions that rely on Windows language.
            Encoding encoding = formatEncoding ?? Encoding.GetEncoding(Encoding.ASCII.CodePage, new EncoderExceptionFallback(), new DecoderExceptionFallback());

            using (MemoryStream memoryStream = new MemoryStream())
            using (TextWriter memoryWriter = new StreamWriter(memoryStream, encoding))
            {
                // First copy the model to memory, if any exceptions occur this
                // won't corrupt the original file.
                SlnFileV12Writer slnWriter = new(model, memoryWriter);
                slnWriter.WriteSolution(ShouldWriteExtraHeaderLine(encoding));
                await memoryWriter.FlushAsync();

                // Copy the memory stream to the output.
                memoryStream.Position = 0;
                await memoryStream.CopyToAsync(streamWriter);
                streamWriter.SetLength(streamWriter.Position);
            }
        }

        private static bool ShouldWriteExtraHeaderLine(Encoding encoding)
        {
            byte[]? bom = encoding.GetPreamble();
            return bom is not null && bom.Length > 0;
        }

        public void WriteSolution(bool writeExtraLine)
        {
            if (writeExtraLine)
            {
                // The old code wrote an extra new line if a BOM was written.
                writer.WriteLine();
            }

            // emits "Microsoft Visual Studio Solution File, Format Version 12.00";
            writer.Write(SlnConstants.SLNFileHeaderNoVersion);            //  Microsoft Visual Studio Solution File, Format Version
            writer.WriteLine(SlnConstants.SLNFileHeaderVersion);          //  Microsoft Visual Studio Solution File, Format Version 12.00

            string? openWithVS = model.GetOpenWithVisualStudio();
            if (openWithVS is not null)
            {
                writer.Write(SlnConstants.OpenWithPrefix);
                writer.WriteLine(openWithVS);
            }

            if (!string.IsNullOrEmpty(model.VsVersion))
            {
                writer.Write(SlnConstants.TagVisualStudioVersion);  // VisualStudioVersion
                writer.Write(SlnConstants.TagAssignValue);          // VisualStudioVersion =
                writer.WriteLine(model.VsVersion);                  // VisualStudioVersion = [ver]
            }

            if (!string.IsNullOrEmpty(model.MinVsVersion))
            {
                writer.Write(SlnConstants.TagMinimumVisualStudioVersion);   // MinimumVisualStudioVersion
                writer.Write(SlnConstants.TagAssignValue);                  // MinimumVisualStudioVersion =
                writer.WriteLine(model.MinVsVersion);                       // MinimumVisualStudioVersion = [ver]
            }

            foreach (SolutionItemModel item in model.SolutionItems)
            {
                this.WriteProject(model, item);
            }

            writer.WriteLine(SlnConstants.TagGlobal);          // Global

            foreach (SolutionPropertyBag section in model.GetSlnProperties())
            {
                this.WritePropertyMap(isSolution: true, section);
            }

            writer.WriteLine(SlnConstants.TagEndGlobal);      // EndGlobal
        }

        private void WritePropertyMap(bool isSolution, SolutionPropertyBag map)
        {
            this.WritePropertyMap(map.Id, isSolution, map.Scope, map);
        }

        private void WritePropertyMap(string id, bool isSolution, PropertiesScope scope, IReadOnlyDictionary<string, string> properties)
        {
            if (string.IsNullOrEmpty(id))
            {
                return;
            }

            if (properties.Count == 0)
            {
                // old parser actually do not write empty maps. We should have already filtered these out from the model, but just in case.
                // Todo: might consider preserving the empty maps if there are comments associated with it.
                // to allow placeholders template solution file? But we really do not promote self editing these, so likely no user case.
                return;
            }

            using (this.WriteSectionHeader(isSolution, id, scope))
            {
                foreach ((string propName, string propValue) in properties)
                {
                    this.WriteProperty(propName, propValue);
                }
            }
        }

        private void WriteProject(SolutionModel solutionModel, SolutionItemModel item)
        {
            // For solution folders, path is just the display name again.
            string path = item is SolutionProjectModel project ? project.FilePath : item.CanonicalDisplayName;

            if (item.TypeId == Guid.Empty)
            {
                throw new InvalidOperationException("Missing essential property TypeId on project.");
            }
            else if (string.IsNullOrEmpty(item.CanonicalDisplayName))
            {
                throw new InvalidOperationException("Missing essential property DisplayName on project.");
            }
            else if (string.IsNullOrEmpty(path))
            {
                throw new InvalidOperationException("Missing essential property FilePath on project.");
            }
            else if (item.Id == Guid.Empty)
            {
                throw new InvalidOperationException("Missing essential property Id on project");
            }

            writer.Write(SlnConstants.TagProject);          // Project
            writer.Write(@"(""");                           // Project("
            writer.Write(item.TypeId.ToSlnString());        // Project("[type]
            writer.Write(@""") = """);                      // Project("[type]") = ")
            writer.Write(item.CanonicalDisplayName);        // Project("[type]") = "[dispName])
            writer.Write(SlnConstants.TagQuoteCommaQuote);  // Project("[type]") = "[dispName]", "
            writer.Write(path);                             // Project("[type]") = "[dispName]", "[relpath]
            writer.Write(SlnConstants.TagQuoteCommaQuote);  // Project("[type]") = "[dispName]", "[relpath]", "
            writer.Write(item.Id.ToSlnString());            // Project("[type]") = "[dispName]", "[relpath]", "[guid]
            writer.WriteLine('\"');                         // Project("[type]") = "[dispName]", "[relpath]", "[guid]"

            foreach (SolutionPropertyBag map in item.GetSlnProperties(solutionModel))
            {
                this.WritePropertyMap(isSolution: false, map);
            }

            writer.WriteLine(SlnConstants.TagEndProject); // EndProject
        }

        private void WriteProperty(string name, string value)
        {
            writer.Write("\t\t");      // <tab><tab>
            writer.Write(name);        // <tab><tab>[propName]
            writer.Write(" = ");       // <tab><tab>[propName] =
            writer.WriteLine(value);   // <tab><tab>[propName] = [propValue]
        }

        private WriteSectionScope WriteSectionHeader(bool isSolution, string id, PropertiesScope scope)
        {
            string sectionTag = isSolution ? "GlobalSection" : "ProjectSection";
            string sectionScope = scope == PropertiesScope.PostLoad ?
                isSolution ? "postSolution" : "postProject" :
                isSolution ? "preSolution" : "preProject";

            writer.Write('\t');
            writer.Write(sectionTag);
            writer.Write('(');
            writer.Write(id);
            writer.Write(") = ");
            writer.WriteLine(sectionScope);
            return new WriteSectionScope(writer, sectionTag);
        }

        // Scope to make sure end tags are written to sections.
        private readonly ref struct WriteSectionScope(TextWriter writer, string sectionTag)
        {
            public void Dispose()
            {
                writer.Write("\tEnd");
                writer.WriteLine(sectionTag);
            }
        }
    }
}
