// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.SolutionPersistence;

internal static class StringExtensions
{
    /// <inheritdoc cref="string.IsNullOrEmpty(string)"/>
    public static bool IsNullOrEmpty([NotNullWhen(returnValue: false)] this string? s) => string.IsNullOrEmpty(s);

    public static string? NullIfEmpty(this string? str) => IsNullOrEmpty(str) ? null : str;

    public static int IndexOf(this StringSpan span, string str) => span.IndexOf(str.AsSpan());

    public static bool EqualsOrdinal(this StringSpan span, string str) => EqualsOrdinal(span, str.AsSpan());

    public static bool EqualsOrdinal(this StringSpan span, StringSpan str) => MemoryExtensions.Equals(span, str, StringComparison.Ordinal);

    public static bool EqualsOrdinalIgnoreCase(this StringSpan span, string str) => EqualsOrdinalIgnoreCase(span, str.AsSpan());

    public static bool EqualsOrdinalIgnoreCase(this StringSpan span, StringSpan str) => MemoryExtensions.Equals(span, str, StringComparison.OrdinalIgnoreCase);

    public static bool StartsWith(this StringSpan span, string str) => span.StartsWith(str.AsSpan());

#if NETFRAMEWORK

    public static bool StartsWith(this string str, char value) => !str.IsNullOrEmpty() && str[0] == value;

    public static bool EndsWith(this string str, char value) => !str.IsNullOrEmpty() && str[str.Length - 1] == value;

    public static bool Contains(this StringSpan span, char c)
    {
        return span.IndexOf(c) >= 0;
    }

#endif

    public static string Concat(scoped StringSpan first, scoped StringSpan second)
    {
#if NETFRAMEWORK
        return
            first.IsEmpty ? second.ToString() :
            second.IsEmpty ? first.ToString() :
            AlwaysConcat(first, second);
#else
        return string.Concat(first, second);
#endif
    }

#if NETFRAMEWORK

    public static void Write(this TextWriter writer, StringSpan str)
    {
        writer.Write(str.ToString());
    }

    public static void WriteLine(this TextWriter writer, StringSpan str)
    {
        writer.WriteLine(str.ToString());
    }

    public static void Deconstruct<TKey, TValue>(this KeyValuePair<TKey, TValue> pair, out TKey key, out TValue value)
    {
        key = pair.Key;
        value = pair.Value;
    }

#endif

    private static string AlwaysConcat(StringSpan first, StringSpan second)
    {
        int newLength = first.Length + second.Length;
        Span<char> buffer = newLength <= 1024 ? stackalloc char[newLength] : new char[newLength];
        first.CopyTo(buffer);
        second.CopyTo(buffer.Slice(first.Length));
        return buffer.ToString();
    }
}
