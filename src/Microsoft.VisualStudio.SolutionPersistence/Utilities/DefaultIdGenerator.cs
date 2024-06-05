// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;

namespace Microsoft.VisualStudio.SolutionPersistence.Utilities;

/// <summary>
/// Helper for turning unique string identifiers into guid identifiers.
/// </summary>
internal readonly struct DefaultIdGenerator
{
    private readonly IncrementalHash hash;

    public DefaultIdGenerator()
    {
        this.hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
    }

    public Guid CreateIdFrom(string uniqueName)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(uniqueName.ToUpperInvariant());
        return this.MakeId(bytes, null);
    }

    public Guid CreateIdFrom(Guid parentItemId, string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return Guid.Empty;
        }

        byte[] parentData = parentItemId.ToByteArray();
        byte[] itemData = Encoding.UTF8.GetBytes(name.ToUpperInvariant());
        return this.MakeId(parentData, itemData);
    }

    private Guid MakeId(byte[]? data1, byte[]? data2)
    {
        if (data1.IsNullOrEmpty() && data2.IsNullOrEmpty())
        {
            return Guid.Empty;
        }

        if (!data1.IsNullOrEmpty())
        {
            this.hash.AppendData(data1);
        }

        if (!data2.IsNullOrEmpty())
        {
            this.hash.AppendData(data2);
        }

        byte[] hash = this.hash.GetHashAndReset();
        byte[] guidBytes = new byte[16];
        Array.Copy(hash, guidBytes, 16);
        return new Guid(guidBytes);
    }
}
