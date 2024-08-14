// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.VisualStudio.SolutionPersistence.Serializer.Xml;

namespace Serialization;

/// <summary>
/// Tests related to legacy solution values.
/// </summary>
public sealed class LegacyValues
{
    /// <summary>
    /// Ensure the legacy VS properties are removed from .sln files when converting to .slnx.
    /// </summary>
    [Fact]
    public async Task TrimVisualStudioPropertiesAsync()
    {
        SolutionModel model = await SolutionSerializers.SlnXml.OpenAsync(SlnAssets.XmlSlnxLegacyValues.Stream, CancellationToken.None);

        Assert.True(model.TryGetSettings(out SlnxSerializerSettings oldSettings));

        ResourceStream expectedCleaned = SlnAssets.XmlSlnxLegacyValuesTrimVS;

        model.SerializerExtension = SolutionSerializers.SlnXml.CreateModelExtension(
            new SlnxSerializerSettings(oldSettings)
            {
                PreserveWhitespace = false,
                TrimVisualStudioProperties = true,
            });

        (SolutionModel _, FileContents cleaned) = await SaveAndReopenModelAsync(SolutionSerializers.SlnXml, model);

        AssertSolutionsAreEqual(expectedCleaned.ToLines(), cleaned);
    }
}
