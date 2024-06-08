// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq;
using Microsoft.VisualStudio.SolutionPersistence;
using Microsoft.VisualStudio.SolutionPersistence.Model;
using Microsoft.VisualStudio.SolutionPersistence.Serializer;
using Microsoft.VisualStudio.SolutionPersistence.Serializer.SlnV12;
using Microsoft.VisualStudio.SolutionPersistence.Serializer.Xml;
using Microsoft.VisualStudio.SolutionPersistence.Serializer.Xml.XmlDecorators;
using Xunit;

namespace Utilities;

/// <summary>
/// Helper methods to be included in sln/slnx tests.
/// </summary>
internal static partial class SlnTestHelper
{
    public static async Task<FileContents> ModelToLinesAsync<T>(ISolutionSingleFileSerializer<T> serializer, SolutionModel updateModel, string name, int bufferSize = 1024 * 1024)
    {
        byte[] buffer = new byte[bufferSize];
        using (MemoryStream memoryStream = new MemoryStream(buffer))
        {
            await serializer.SaveAsync(name, memoryStream, updateModel, CancellationToken.None);
        }

        return buffer.ToLines();
    }

    /// <summary>
    /// Helper to wrap the common pattern of creating a new model and updating it.
    /// CONSIDER: Should this helper be on the model.
    /// </summary>
    public static SolutionModel CreateNew(this SolutionModel compactSolutionModel, Action<SolutionModel.Builder> modifyBuilder)
    {
        SolutionModel.Builder builder = new SolutionModel.Builder(compactSolutionModel, stringTable: null);
        modifyBuilder(builder);
        return builder.ToModel(compactSolutionModel.SerializerExtension);
    }

    /// <summary>
    /// Save the model using the serializer then reload it into a new model.
    /// </summary>
    public static async Task<(SolutionModel Model, FileContents Contents)> SaveAndReopenModelAsync<T>(
        ISolutionSingleFileSerializer<T> serializer,
        SolutionModel oldModel,
        string name,
        int bufferSize = 1024 * 1024)
    {
        byte[] memoryBuffer = new byte[bufferSize];
        using (MemoryStream saveStream = new MemoryStream(memoryBuffer))
        using (MemoryStream openStream = new MemoryStream(memoryBuffer))
        {
            await serializer.SaveAsync(name, saveStream, oldModel, CancellationToken.None);

            // Use the length of the save stream to set the length of the open stream.
            openStream.SetLength(saveStream.Position);

            FileContents slnxContents = saveStream.ToLines();

            SolutionModel newModel = await serializer.OpenAsync(name, openStream, CancellationToken.None);
            return (newModel, slnxContents);
        }
    }

    public static async Task<(SolutionModel Model, FileContents SlnxContents)> ThruSlnStreamAsync(SolutionModel model, string name, int bufferSize)
    {
        ISerializerModelExtension originalExtension = model.SerializerExtension;
        Assert.NotNull(originalExtension);

        // When converting to slnx, the order of projects will change (since they are grouped by folders).
        // This captures the order they are in for the .sln so it can be restored to get rid of diff noise.
        IReadOnlyList<Guid> originalOrder = model.SolutionItems.ToArray(x => x.Id);

        // Keep info that isn't serialized.
        string? vsVersion = model.VsVersion;
        string? minVersion = model.MinVsVersion;
        Guid? solutionGuid = model.SolutionId;
        Dictionary<string, Guid> itemGuids = model.SolutionItems.ToDictionary(x => x.ItemRef, x => x.Id);

        (model, FileContents slnxContents) = await SaveAndReopenModelAsync(SolutionSerializers.SlnXml, model, name, bufferSize);

        foreach (KeyValuePair<string, Guid> projectGuid in itemGuids)
        {
            FindItem(model, projectGuid.Key).Id = projectGuid.Value;
        }

        // Restore info that isn't serialized to make diff match.
        model = model with
        {
            VsVersion = vsVersion,
            MinVsVersion = minVersion,
            SolutionId = solutionGuid,
        };

        // This is hacky, but need to preserve original order to make test diff work.
        List<SolutionItemModel> itemsHack = (List<SolutionItemModel>)model.SolutionItems;
        List<SolutionProjectModel> projectsHack = (List<SolutionProjectModel>)model.SolutionProjects;
        List<SolutionFolderModel> foldersHack = (List<SolutionFolderModel>)model.SolutionFolders;

        itemsHack.Sort((a, b) => originalOrder.IndexOf(a.Id).CompareTo(originalOrder.IndexOf(b.Id)));
        projectsHack.Sort((a, b) => originalOrder.IndexOf(a.Id).CompareTo(originalOrder.IndexOf(b.Id)));
        foldersHack.Sort((a, b) => originalOrder.IndexOf(a.Id).CompareTo(originalOrder.IndexOf(b.Id)));

        // Rehydrate some expected lost information when converting.
        model = model with
        {
            SerializerExtension = originalExtension,
        };
        return (model, slnxContents);

        static SolutionItemModel FindItem(SolutionModel compactModel, string itemRef)
        {
            try
            {
                return compactModel.SolutionItems.First(x => x.ItemRef == itemRef);
            }
            catch (Exception)
            {
                throw new InvalidOperationException($"Project {itemRef} not found!");
            }
        }
    }

    public static void AssertSolutionsAreEqual(
        FileContents expectedSln,
        FileContents actualSln)
    {
        int count = Math.Min(expectedSln.Lines.Count, actualSln.Lines.Count);
        for (int i = 0; i < count; i++)
        {
            string expectedLine = expectedSln.Lines[i];
            string actualLine = actualSln.Lines[i];

            // Don't build the error message if the lines match.
            if (string.Equals(expectedLine, actualLine, StringComparison.Ordinal))
            {
                continue;
            }

            // Find the index where the strings do not match
            StringBuilder pointer = new StringBuilder(expectedLine.Length + 3);
            int minLength = Math.Min(expectedLine.Length, actualLine.Length);
            for (int sameCount = 0; sameCount < minLength && expectedLine[sameCount] == actualLine[sameCount]; sameCount++)
            {
                _ = pointer.Append(expectedLine[sameCount] == '\t' ? '\t' : '-');
            }

            _ = pointer.Append("^^");

            Assert.Fail(
                $"""
                Solution lines #{i + 1} do not match.
                {expectedLine}
                {actualLine}
                {pointer}
                EXPECTED:
                {expectedSln.FullString}
                ACTUAL:
                {actualSln.FullString}
                """);
        }

        Assert.Equal(expectedSln.FullString, actualSln.FullString);
    }

    // This only works for SLNX right now.
    public static void AssertEmptySerializationLog(SolutionModel model)
    {
        SlnxFile? slnxDOM = ((SlnXmlModelExtension)model.SerializerExtension).Root;
        Assert.NotNull(slnxDOM);
        Assert.Equal(slnxDOM.Logger.ToString(), string.Empty);
    }

    public static FileContents ToLines(this byte[] buffer)
    {
        int length = Array.IndexOf(buffer, (byte)0);
        using (MemoryStream stream = new MemoryStream(buffer))
        {
            stream.SetLength(length);
            return stream.ToLines();
        }
    }

    public static FileContents ToLines(this ResourceStream resource)
    {
        return resource.Stream.ToLines();
    }

    private static FileContents ToLines(this Stream stream)
    {
        stream.Position = 0;
        using StreamReader reader = new StreamReader(stream, encoding: Encoding.UTF8, detectEncodingFromByteOrderMarks: true, bufferSize: 1024, leaveOpen: true);
        string fullString = reader.ReadToEnd();
        stream.Position = 0;
        List<string> lines = new List<string>(1024);
        while (reader.ReadLine() is string line)
        {
            lines.Add(line);
        }

        stream.Position = 0;

        return (fullString, lines);
    }

    public static void RecreateProjectConfigurations(this SolutionModel.Builder builder, SolutionModel model)
    {
        // This similuates what VS does, but isn't the most efficient way to recalculate rules.
        IEnumerable<SolutionPropertyBag> slnProperties = model.GetSlnProperties();

        foreach (SolutionPropertyBag slnPropertyBag in slnProperties)
        {
            builder.AddSlnProperties(slnPropertyBag);
        }
    }
}
