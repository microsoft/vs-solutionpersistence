﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Xml;
using Microsoft.VisualStudio.SolutionPersistence.Model;

namespace Microsoft.VisualStudio.SolutionPersistence.Serializer.Xml.XmlDecorators;

internal sealed class XmlConfigurationBuildType(SlnxFile root, XmlElement element) :
    XmlConfiguration(root, element, Keyword.BuildType)
{
    internal override BuildDimension Dimension => BuildDimension.BuildType;
}
