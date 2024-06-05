// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.SolutionPersistence.Model;

internal static class BuildTypeNames
{
    public const string All = PlatformNames.All;
    public const string Debug = nameof(Debug);
    public const string Release = nameof(Release);

    public static string ToStringKnown(this StringSpan buildType)
    {
        return buildType switch
        {
            All => All,
            Debug => Debug,
            Release => Release,
            _ => buildType.ToString(),
        };
    }
}
