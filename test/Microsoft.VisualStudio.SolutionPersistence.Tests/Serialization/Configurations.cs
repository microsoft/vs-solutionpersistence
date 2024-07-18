// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.VisualStudio.SolutionPersistence.Model;
using Xunit;

namespace Serialization;

public class Configurations
{
    /// <summary>
    /// Tests the <see cref="SolutionProjectModel.GetProjectConfiguration(string, string)"/> API.
    /// </summary>
    [Fact]
    public void ProjectConfigurations()
    {
        const string platform = "TestPlatform";
        const string buildType = "Debug";
        const string projectPlatform = "ProjectTestPlatform";

        // Create a simple solution with a single project.
        SolutionModel solutionModel = new SolutionModel();

        solutionModel.AddPlatform(platform);
        solutionModel.AddPlatform("Any CPU");

        solutionModel.AddBuildType(buildType);
        solutionModel.AddBuildType("Release");

        SolutionProjectModel project = solutionModel.AddProject(@"Foo\Foo.csproj", null);

        // Add some project configurations for a specific solution configuration.
        project.AddProjectConfigurationRule(new ConfigurationRule(BuildDimension.Build, buildType, platform, bool.TrueString));
        project.AddProjectConfigurationRule(new ConfigurationRule(BuildDimension.Deploy, buildType, platform, bool.TrueString));
        project.AddProjectConfigurationRule(new ConfigurationRule(BuildDimension.BuildType, buildType, platform, buildType));
        project.AddProjectConfigurationRule(new ConfigurationRule(BuildDimension.Platform, buildType, platform, projectPlatform));

        Assert.NotNull(project.ProjectConfigurationRules);
        Assert.Equal(4, project.ProjectConfigurationRules.Count);

        // Remove implied configurations.
        solutionModel.DistillProjectConfigurations();

        Assert.Equal(2, project.ProjectConfigurationRules.Count);

        // Verify set configurations are still there.
        (string BuildType, string Platform, bool Build, bool Deploy) projectConfig = project.GetProjectConfiguration(buildType, platform);
        Assert.True(projectConfig.Build);
        Assert.True(projectConfig.Deploy);
        Assert.Equal(buildType, projectConfig.BuildType);
        Assert.Equal(projectPlatform, projectConfig.Platform);
    }
}
