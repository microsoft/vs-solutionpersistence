// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.SolutionPersistence.Utilities;

internal static class PathExtensions
{
    private static bool IsUri(this StringSpan filePath) => !filePath.IsEmpty && filePath.Contains("://".AsSpan(), StringComparison.Ordinal);

    private static readonly bool IsWindows = Environment.OSVersion.Platform == PlatformID.Win32NT;

    [return: NotNullIfNotNull(nameof(persistencePath))]
    internal static string? ConvertFromPersistencePath(string? persistencePath)
    {
        return persistencePath.IsNullOrEmpty() || IsWindows || !persistencePath.Contains('\\') ?
            persistencePath :
            persistencePath.Replace('\\', Path.DirectorySeparatorChar);
    }

    [return: NotNullIfNotNull(nameof(modelPath))]
    internal static string? ConvertToPersistencePath(string? modelPath)
    {
        return modelPath is null || IsWindows || !modelPath.Contains(Path.DirectorySeparatorChar) || IsUri(modelPath.AsSpan()) ?
            modelPath :
            modelPath.Replace(Path.DirectorySeparatorChar, '\\');
    }

    public static StringSpan GetStandardDisplayName(string filePath)
    {
        return GetStandardDisplayName(filePath.AsSpan());
    }

    public static StringSpan GetStandardDisplayName(StringSpan filePath)
    {
        if (filePath.IsEmpty || filePath.IsUri())
        {
            return StringSpan.Empty;
        }

        return Path.GetFileNameWithoutExtension(filePath);
    }

    public static StringSpan GetExtension(string filePath)
    {
        return GetExtension(filePath.AsSpan());
    }

    public static StringSpan GetExtension(StringSpan filePath)
    {
        return filePath.IsUri() ? StringSpan.Empty : Path.GetExtension(filePath);
    }
}
