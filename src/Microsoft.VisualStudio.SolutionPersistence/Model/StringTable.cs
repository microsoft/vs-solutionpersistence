// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.SolutionPersistence.Model;

public sealed class StringTable
{
    // string deduplication facility (we can expect a lot of similar strings in solution files we want to compact while building the model).
    private readonly HashSet<string> strings = new HashSet<string>(StringComparer.Ordinal);

    public StringTable()
    {
    }

    public string GetString(StringSpan str)
    {
        if (str.IsEmpty)
        {
            return string.Empty;
        }

        return this.GetString(str.ToString());
    }

    [return: NotNullIfNotNull(nameof(str))]
    public string? GetString(string? str)
    {
        if (str is null)
        {
            return null;
        }

        if (str.Length == 0)
        {
            return string.Empty;
        }

        if (this.strings.TryGetValue(str, out string? result))
        {
            return result;
        }

        _ = this.strings.Add(str);
        return str;
    }

    internal void AddString(string str)
    {
        _ = this.GetString(str);
    }
}
