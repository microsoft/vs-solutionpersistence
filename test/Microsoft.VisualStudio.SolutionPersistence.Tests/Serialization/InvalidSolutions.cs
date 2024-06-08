// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Xml;
using Microsoft.VisualStudio.SolutionPersistence.Serializer;
using Utilities;
using Xunit;

namespace Serialization;

/// <summary>
/// These tests validate that errors are reported when trying to open invalid solution files.
/// </summary>
public class InvalidSolutions
{
    [Fact]
    public Task InvalidXmlSlnxAsync()
    {
        return Assert.ThrowsAsync<XmlException>(async () =>
        {
            using MemoryStream memoryStream = new(Encoding.UTF8.GetBytes("Invalid slnx file"));
            _ = await SolutionSerializers.SlnXml.OpenAsync("error.slnx", memoryStream, CancellationToken.None);
        });
    }

    [Fact]
    public Task InvalidSlnxAsync()
    {
        return Assert.ThrowsAsync<InvalidSolutionFormatException>(async () =>
        {
            using MemoryStream memoryStream = new(Encoding.UTF8.GetBytes("<InvalidElement />"));
            _ = await SolutionSerializers.SlnXml.OpenAsync("error.slnx", memoryStream, CancellationToken.None);
        });
    }

    [Fact]
    public Task InvalidSlnAsync()
    {
        return Assert.ThrowsAsync<InvalidSolutionFormatException>(async () =>
        {
            using MemoryStream memoryStream = new(Encoding.UTF8.GetBytes("This is an invalid sln file"));
            _ = await SolutionSerializers.SlnFileV12.OpenAsync("error.sln", memoryStream, CancellationToken.None);
        });
    }

    [Fact]
    public Task InvalidConfigurationLoopAsync()
    {
        return Assert.ThrowsAsync<InvalidSolutionFormatException>(async () =>
        {
            ResourceStream x = SlnAssets.LoadResource(@"Invalid\BasedOnLoop.slnx");
            _ = await SolutionSerializers.SlnXml.OpenAsync(x.Name, x.Stream, CancellationToken.None);
        });
    }
}
