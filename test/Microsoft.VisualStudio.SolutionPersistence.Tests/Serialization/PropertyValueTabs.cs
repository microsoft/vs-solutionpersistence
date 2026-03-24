// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Serialization;

/// <summary>
/// Tests that property values containing tab characters survive serialization roundtrips.
/// </summary>
public class PropertyValueTabs
{
    /// <summary>
    /// Verifies that leading, trailing, and embedded tabs in property values survive a .slnx roundtrip.
    /// </summary>
    [Fact]
    public async Task SlnxRoundTrip_TabsInPropertyValuesAsync()
    {
        SolutionModel solution = new SolutionModel();
        SolutionPropertyBag props = solution.AddProperties("TestProps");
        props.Add("LeadingTab", "\tHello");
        props.Add("TrailingTab", "Hello\t");
        props.Add("EmbeddedTabs", "foo=1\tbaz=value\tbar=2\t");

        (SolutionModel reopened, FileContents _) = await SaveAndReopenModelAsync(SolutionSerializers.SlnXml, solution);

        SolutionPropertyBag? roundTripped = reopened.FindProperties("TestProps");
        Assert.NotNull(roundTripped);
        Assert.Equal("\tHello", roundTripped["LeadingTab"]);
        Assert.Equal("Hello\t", roundTripped["TrailingTab"]);
        Assert.Equal("foo=1\tbaz=value\tbar=2\t", roundTripped["EmbeddedTabs"]);
    }

    /// <summary>
    /// Verifies that leading, trailing, and embedded tabs in property values survive a .sln roundtrip.
    /// </summary>
    [Fact]
    public async Task SlnRoundTrip_TabsInPropertyValuesAsync()
    {
        SolutionModel solution = new SolutionModel();
        SolutionPropertyBag props = solution.AddProperties("TestProps");
        props.Add("LeadingTab", "\tHello");
        props.Add("TrailingTab", "Hello\t");
        props.Add("EmbeddedTabs", "foo=1\tbaz=value\tbar=2\t");
        solution.SerializerExtension = SolutionSerializers.SlnFileV12.CreateModelExtension();

        (SolutionModel reopened, FileContents _) = await SaveAndReopenModelAsync(SolutionSerializers.SlnFileV12, solution);

        SolutionPropertyBag? roundTripped = reopened.FindProperties("TestProps");
        Assert.NotNull(roundTripped);
        Assert.Equal("\tHello", roundTripped["LeadingTab"]);
        Assert.Equal("Hello\t", roundTripped["TrailingTab"]);
        Assert.Equal("foo=1\tbaz=value\tbar=2\t", roundTripped["EmbeddedTabs"]);
    }
}
