﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.VisualStudio.SolutionPersistence.Model;

namespace Microsoft.VisualStudio.SolutionPersistence.Serializer;

internal abstract class SingleFileSerializerBase<TSettings> : ISolutionSingleFileSerializer<TSettings>
{
    private ISolutionSingleFileSerializer<TSettings> AsSingleFileSerializer => this;

    public abstract string Name { get; }

    public string DefaultFileExtension => this.FileExtension;

    public abstract ISerializerModelExtension CreateModelExtension();

    public abstract ISerializerModelExtension CreateModelExtension(TSettings settings);

    private protected abstract string FileExtension { get; }

    private protected abstract Task<SolutionModel> ReadModelAsync(string? fullPath, Stream reader, CancellationToken cancellationToken);

    private protected abstract Task WriteModelAsync(string? fullPath, SolutionModel model, Stream writerStream, CancellationToken cancellationToken);

    Task<SolutionModel> ISolutionSingleFileSerializer<TSettings>.OpenAsync(string? fullPath, Stream reader, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return this.ReadModelAsync(fullPath, reader, cancellationToken);
    }

    Task ISolutionSingleFileSerializer<TSettings>.SaveAsync(string? fullPath, Stream writer, SolutionModel model, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return this.WriteModelAsync(fullPath, model, writer, cancellationToken);
    }

    bool ISolutionSerializer.IsSupported(string fullPath)
    {
        return Path.GetExtension(fullPath.AsSpan()).EqualsOrdinalIgnoreCase(this.FileExtension);
    }

    Task<SolutionModel> ISolutionSerializer.OpenAsync(string moniker, CancellationToken cancellationToken)
    {
        using FileStream stream = File.OpenRead(moniker);
        return this.AsSingleFileSerializer.OpenAsync(moniker, stream, cancellationToken);
    }

    async Task ISolutionSerializer.SaveAsync(string moniker, SolutionModel model, CancellationToken cancellationToken)
    {
        string? directory = Path.GetDirectoryName(moniker);
        if (directory is not null && !Directory.Exists(directory))
        {
            _ = Directory.CreateDirectory(directory);
        }

        using (FileStream stream = File.OpenWrite(moniker))
        {
            await this.AsSingleFileSerializer.SaveAsync(moniker, stream, model, cancellationToken);
        }
    }
}