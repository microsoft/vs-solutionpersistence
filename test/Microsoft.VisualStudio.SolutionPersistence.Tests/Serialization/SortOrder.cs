// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Serialization;

/// <summary>
/// Tests related to <see cref="SolutionSortOrder"/>.
/// </summary>
public class SortOrder
{
    /// <summary>
    /// Ensures <see cref="SolutionSortOrder.Document"/> is written to the file and round-trips.
    /// </summary>
    [Fact]
    public async Task DocumentRoundTripsAsync()
    {
        SolutionModel solution = new SolutionModel { SortOrder = SolutionSortOrder.Document };
        _ = solution.AddProject("A.csproj");

        (SolutionModel reserializedSolution, FileContents contents) = await SaveAndReopenModelAsync(SolutionSerializers.SlnXml, solution);

        Assert.Contains("Sort=\"Document\"", contents.FullString);
        Assert.Equal(SolutionSortOrder.Document, reserializedSolution.SortOrder);
    }

    /// <summary>
    /// Ensures <see cref="SolutionSortOrder.Alphabetical"/> is omitted from the file and round-trips.
    /// </summary>
    [Fact]
    public async Task AlphabeticalIsOmittedAsync()
    {
        SolutionModel solution = new SolutionModel();
        _ = solution.AddProject("A.csproj");

        (SolutionModel reserializedSolution, FileContents contents) = await SaveAndReopenModelAsync(SolutionSerializers.SlnXml, solution);

        Assert.DoesNotContain("Sort=", contents.FullString);
        Assert.Equal(SolutionSortOrder.Alphabetical, reserializedSolution.SortOrder);
    }

    /// <summary>
    /// Ensures an unrecognized Sort value is treated as <see cref="SolutionSortOrder.Alphabetical"/>.
    /// </summary>
    [Fact]
    public async Task UnknownSortValueIsAlphabeticalAsync()
    {
        const string slnx = """
<Solution Sort="Bogus">
  <Project Path="A.csproj" />
</Solution>
""";

        using MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(slnx));
        SolutionModel solution = await SolutionSerializers.SlnXml.OpenAsync(stream, CancellationToken.None);

        Assert.Equal(SolutionSortOrder.Alphabetical, solution.SortOrder);
    }

    /// <summary>
    /// Ensures the Sort value is parsed case-insensitively.
    /// </summary>
    [Fact]
    public async Task SortValueIsCaseInsensitiveAsync()
    {
        const string slnx = """
<Solution Sort="document">
  <Project Path="A.csproj" />
</Solution>
""";

        using MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(slnx));
        SolutionModel solution = await SolutionSerializers.SlnXml.OpenAsync(stream, CancellationToken.None);

        Assert.Equal(SolutionSortOrder.Document, solution.SortOrder);
    }
}
