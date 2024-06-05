// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.SolutionPersistence.Model;

internal static class PlatformNames
{
    public const string All = "*";

    public const string AnyCPU = nameof(AnyCPU);
    public const string AnySpaceCPU = "Any CPU";
    public const string Win32 = nameof(Win32);
#pragma warning disable SA1303 // Const field names should begin with upper-case letter
    public const string x64 = nameof(x64);
    public const string x86 = nameof(x86);
    public const string arm = nameof(arm);
    public const string arm64 = nameof(arm64);
#pragma warning restore SA1303 // Const field names should begin with upper-case letter

    // All caps to intern this common version.
    public const string ARM = nameof(ARM);
    public const string ARM64 = nameof(ARM64);

    public static string Canonical(this string platform) => string.Equals(platform, AnySpaceCPU, StringComparison.OrdinalIgnoreCase) ? AnyCPU : platform;

    public static string ToStringKnown(this StringSpan platform)
    {
        return platform switch
        {
            All => All,
            AnySpaceCPU => AnySpaceCPU,
            Win32 => Win32,
            x64 => x64,
            x86 => x86,
            arm => arm,
            arm64 => arm64,
            ARM => ARM,
            ARM64 => ARM64,
            _ => platform.ToString(),
        };
    }
}
