﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.SolutionPersistence;
using Utilities;

namespace Utilities;

internal static class SlnAssets
{
    private const string SlnAssetsRoot = "SlnAssets.";

    #region Sample Classic Sln

    // Empty Solution. ASCII encoding.
    public static ResourceStream ClassicSlnBlank => LoadResource("BlankSolution.sln");

    // Medium SDK-style csproj solution. UTF8 BOM encoding.
    public static ResourceStream ClassicSlnCps => LoadResource("cps.sln");

    // Sample solution with multiple project types and shared project. UTF8 BOM encoding.
    public static ResourceStream ClassicSlnEverything => LoadResource("Everything.sln");

    public static ResourceStream ClassicSlnMany => LoadResource("SampleMany.sln");

    // Big SDK-style csproj solution. ASCII encoding.
    public static ResourceStream ClassicSlnOrchardCore => LoadResource("OrchardCore.sln");

    // A single C++ project with "Mobile"->"ARM64" platform, doesn't and build ARM64 sln platform. UTF8 BOM encoding.
    public static ResourceStream ClassicSlnSingleNativeProject => LoadResource("SingleNativeProject.sln");

    private static readonly Assembly ResourceAssembly = typeof(SlnAssets).Assembly;

    public static IEnumerable<ResourceName> GetAllSampleFiles(string postFix)
    {
        string[] allResources = ResourceAssembly.GetManifestResourceNames();
        foreach (string fullResourceId in allResources)
        {
            StringSpan resourceName = fullResourceId.AsSpan();
            if (!resourceName.StartsWith(SlnAssetsRoot))
            {
                continue;
            }

            resourceName = resourceName.Slice(SlnAssetsRoot.Length);

            if (!resourceName.StartsWithIgnoreCase("Invalid") &&
                (resourceName.EndsWith(postFix + ".txt") || resourceName.EndsWith(postFix + ".xml")))
            {
                Stream? stream = ResourceAssembly.GetManifestResourceStream(fullResourceId);
                if (stream is not null)
                {
                    yield return new ResourceName(Path.GetFileNameWithoutExtension(resourceName).ToString(), fullResourceId);
                }
            }
        }
    }

    public static ResourceStream Load(this ResourceName resourceName)
    {
        return new ResourceStream(resourceName.Name, ResourceAssembly.GetManifestResourceStream(resourceName.FullResourceId)!);
    }

    public static ResourceName[] ClassicSlnFiles => GetAllSampleFiles(".sln").ToArray();

    #endregion

    #region Sample Xml Slnx

    // Solution with comments and user XML.
    public static ResourceStream XmlSlnxComments => LoadResource("Comments.slnx");

    // Solution with just a property bag and user XML.
    public static ResourceStream XmlSlnxJustProperties => LoadResource("JustProperties.slnx");

    // A single C++ project with "Mobile"->"ARM64" platform, doesn't and build ARM64 sln platform.
    public static ResourceStream XmlSlnxSingleNativeProject => LoadResource("SingleNativeProject.slnx");

    // Empty Solution.
    public static ResourceStream XmlSlnxBlank => LoadResource("BlankSolution.slnx");

    // Medium SDK-style csproj solution.
    public static ResourceStream XmlSlnxCps => LoadResource("cps.slnx");

    // Sample solution with multiple project types.
    public static ResourceStream XmlSlnxEverything => LoadResource("Everything.slnx");

    public static ResourceStream XmlSlnxMany => LoadResource("SampleMany.slnx");

    // Big SDK-style csproj solution.
    public static ResourceStream XmlSlnxOrchardCore => LoadResource("OrchardCore.slnx");

    // Metadata for known project types.
    public static ResourceStream XmlBuiltInProjectTypes => LoadResource(@"Configurations\BuiltInProjectTypes.slnx");

    public static ResourceName[] XmlSlnxFiles => GetAllSampleFiles(".slnx").ToArray();

    #endregion

    #region Test result Slnx

    public static ResourceStream XmlSlnxProperties_Empty => LoadResource(@"Properties\JustProperties-empty.slnx");

    public static ResourceStream XmlSlnxProperties_Add0Add7 => LoadResource(@"Properties\JustProperties-add0add7.slnx");

    public static ResourceStream XmlSlnxProperties_No2No4 => LoadResource(@"Properties\JustProperties-no2no4.slnx");

    public static ResourceStream XmlSlnxProperties_NoComments => LoadResource(@"Properties\JustProperties-nocomments.slnx");

    #endregion

    public static ResourceStream LoadResource(string name)
    {
        name = name.Replace('\\', '.');
        Stream? stream =
            ResourceAssembly.GetManifestResourceStream(SlnAssetsRoot + name + ".txt") ??
            ResourceAssembly.GetManifestResourceStream(SlnAssetsRoot + name + ".xml");
        if (stream is not null)
        {
            return new ResourceStream(name, stream);
        }

        // Create an error message to help diagnose the missing resource.
        string[] allResources = ResourceAssembly.GetManifestResourceNames();
        StringBuilder errorMessage = new StringBuilder($"Resource '{name}' not found.");
        _ = errorMessage.AppendLine("Resource found:");
        foreach (string resource in allResources)
        {
            _ = errorMessage.AppendLine(resource);
        }

        throw new InvalidOperationException(errorMessage.ToString());
    }
}