// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.SolutionPersistence.Model;
using Microsoft.VisualStudio.SolutionPersistence.Utilities;

namespace Microsoft.VisualStudio.SolutionPersistence.Serializer.SlnfJson;

internal sealed partial class SlnfJsonSerializer : SingleFileSerializerBase<SlnfJsonSerializerSettings>
{
    public static ISolutionSingleFileSerializer<SlnfJsonSerializerSettings> Instance => Singleton<SlnfJsonSerializer>.Instance;

    public override string Name => "Slnf";

    private protected override string FileExtension => ".slnf";

    public override ISerializerModelExtension CreateModelExtension()
    {
        return new SlnfJsonModelExtension(this, new SlnfJsonSerializerSettings());
    }

    public override ISerializerModelExtension CreateModelExtension(SlnfJsonSerializerSettings settings)
    {
        return new SlnfJsonModelExtension(this, settings);
    }

    private protected override Task<SolutionModel> ReadModelAsync(string? fullPath, Stream reader, CancellationToken cancellationToken)
    {
        Reader parser = new Reader(fullPath, reader);
        return Task.FromResult(parser.Parse());
    }

    private protected override Task WriteModelAsync(string? fullPath, SolutionModel model, Stream writerStream, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
