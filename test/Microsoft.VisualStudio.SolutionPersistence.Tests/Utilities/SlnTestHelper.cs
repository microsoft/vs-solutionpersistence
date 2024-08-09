// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq;
using Microsoft.VisualStudio.SolutionPersistence.Serializer.SlnV12;

namespace Utilities;

/// <summary>
/// Helper methods to be included in sln/slnx tests.
/// </summary>
internal static class SlnTestHelper
{
    internal static async Task<FileContents> ModelToLinesAsync<T>(ISolutionSingleFileSerializer<T> serializer, SolutionModel updateModel, int bufferSize = 1024 * 1024)
    {
        byte[] buffer = new byte[bufferSize];
        using (MemoryStream memoryStream = new MemoryStream(buffer))
        {
            await serializer.SaveAsync(memoryStream, updateModel, CancellationToken.None);
        }

        return buffer.ToLines();
    }

    /// <summary>
    /// Helper to creating a new model and update it.
    /// </summary>
    internal static SolutionModel CreateCopy(this SolutionModel solution, Action<SolutionModel> modifyModel)
    {
        SolutionModel model = new SolutionModel(solution) { SerializerExtension = solution.SerializerExtension };
        modifyModel(model);
        return model;
    }

    /// <summary>
    /// Save the model using the serializer then reload it into a new model.
    /// </summary>
    internal static async Task<(SolutionModel Model, FileContents Contents)> SaveAndReopenModelAsync<T>(
        ISolutionSingleFileSerializer<T> serializer,
        SolutionModel oldModel,
        int bufferSize = 1024 * 1024)
    {
        byte[] memoryBuffer = new byte[bufferSize];
        using (MemoryStream saveStream = new MemoryStream(memoryBuffer))
        using (MemoryStream openStream = new MemoryStream(memoryBuffer))
        {
            await serializer.SaveAsync(saveStream, oldModel, CancellationToken.None);

            // Use the length of the save stream to set the length of the open stream.
            openStream.SetLength(saveStream.Position);

            FileContents slnxContents = saveStream.ToLines();

            SolutionModel newModel = await serializer.OpenAsync(openStream, CancellationToken.None);
            return (newModel, slnxContents);
        }
    }

    internal static async Task<(SolutionModel Model, FileContents SlnxContents)> ThruSlnxStreamAsync(SolutionModel model, int bufferSize)
    {
        ISerializerModelExtension? originalExtension = model.SerializerExtension;

        // When converting to slnx, the order of projects will change (since they are grouped by folders).
        // This captures the order they are in for the .sln so it can be restored to get rid of diff noise.
        IReadOnlyList<Guid> originalOrder = model.SolutionItems.ToArray(x => x.Id);

        // Keep info that isn't serialized.
        string? vsVersion = model.VsVersion;
        string? minVersion = model.MinVsVersion;
        Guid? solutionGuid = model.SolutionId;
        Dictionary<string, Guid> itemGuids = model.SolutionItems.ToDictionary(x => x.ItemRef, x => x.Id);

        (model, FileContents slnxContents) = await SaveAndReopenModelAsync(SolutionSerializers.SlnXml, model, bufferSize);

        foreach (KeyValuePair<string, Guid> projectGuid in itemGuids)
        {
            FindItem(model, projectGuid.Key).Id = projectGuid.Value;
        }

        // Restore info that isn't serialized to make diff match.
        model.VsVersion = vsVersion;
        model.MinVsVersion = minVersion;
        model.SolutionId = solutionGuid;

        // This is hacky, but need to preserve original order to make test diff work.
        List<SolutionItemModel> itemsHack = (List<SolutionItemModel>)model.SolutionItems;
        List<SolutionProjectModel> projectsHack = (List<SolutionProjectModel>)model.SolutionProjects;
        List<SolutionFolderModel> foldersHack = (List<SolutionFolderModel>)model.SolutionFolders;

        itemsHack.Sort((a, b) => originalOrder.IndexOf(a.Id).CompareTo(originalOrder.IndexOf(b.Id)));
        projectsHack.Sort((a, b) => originalOrder.IndexOf(a.Id).CompareTo(originalOrder.IndexOf(b.Id)));
        foldersHack.Sort((a, b) => originalOrder.IndexOf(a.Id).CompareTo(originalOrder.IndexOf(b.Id)));

        // Rehydrate some expected lost information when converting.
        model.SerializerExtension = originalExtension;
        return (model, slnxContents);

        static SolutionItemModel FindItem(SolutionModel solution, string itemRef)
        {
            return solution.SolutionItems.FindByItemRef(itemRef) ??
                throw new InvalidOperationException($"Project {itemRef} not found!");
        }
    }

    internal static Encoding GetSlnEncoding(SolutionModel model)
    {
        ISerializerModelExtension<SlnV12SerializerSettings>? slnExt = model.SerializerExtension as ISerializerModelExtension<SlnV12SerializerSettings>;

        // Expected SLN serializer for encoding.
        Assert.NotNull(slnExt);

        Encoding? encoding = slnExt.Settings.Encoding;

        Assert.NotNull(encoding);
        return encoding;
    }

    internal static void AssertSolutionsAreEqual(
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

        string expectedSlnFull = expectedSln.FullString;
        if (Environment.NewLine != "\r\n")
        {
            expectedSlnFull = expectedSlnFull.Replace("\r\n", Environment.NewLine);
        }

        Assert.Equal(expectedSlnFull, actualSln.FullString);
    }

    // This only works for SLNX right now.
    internal static void AssertNotTarnished(SolutionModel model)
    {
        Assert.NotNull(model.SerializerExtension);
        Assert.False(model.SerializerExtension.Tarnished);
    }

    internal static void TryDeleteFile(string filePath)
    {
        try
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
        catch
        {
            // Ignore any exceptions.
        }
    }

    internal static FileContents ToLines(this byte[] buffer)
    {
        int length = Array.IndexOf(buffer, (byte)0);
        using (MemoryStream stream = new MemoryStream(buffer))
        {
            stream.SetLength(length);
            return stream.ToLines();
        }
    }

    internal static FileContents ToLines(this ResourceStream resource)
    {
        return resource.Stream.ToLines();
    }

    // Saves the resource to a temp file and returns the path.
    // Used if a test needs to validate reading from disk instead of stream.
    internal static string SaveResourceToTempFile(this ResourceStream resource)
    {
        string filePath = Path.ChangeExtension(Path.GetTempFileName(), resource.Name);

        try
        {
            using (FileStream stream = File.OpenWrite(filePath))
            {
                resource.Stream.CopyTo(stream);
                stream.SetLength(stream.Position);
            }
        }
        catch
        {
            TryDeleteFile(filePath);
            throw;
        }

        return filePath;
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

        return new FileContents(fullString, lines);
    }
}
