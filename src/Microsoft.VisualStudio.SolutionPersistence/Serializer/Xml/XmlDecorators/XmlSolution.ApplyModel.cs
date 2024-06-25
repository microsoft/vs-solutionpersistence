// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq;
using Microsoft.VisualStudio.SolutionPersistence.Model;
using Microsoft.VisualStudio.SolutionPersistence.Utilities;

namespace Microsoft.VisualStudio.SolutionPersistence.Serializer.Xml.XmlDecorators;

/// <summary>
/// Represents the root Solution XML element in the slnx file.
/// These methods are used to update the Xml DOM with changes from the model.
/// </summary>
internal sealed partial class XmlSolution
{
    // Update the Xml DOM with changes from the model.
    internal bool ApplyModelToXml(SolutionModel modelSolution)
    {
        bool modified = false;

        // Attributes
        string description = modelSolution.Description ?? string.Empty;
        if (!StringComparer.Ordinal.Equals(this.Description, description))
        {
            this.Description = description;
            modified = true;
        }

        // Configurations
        // Use the item ref logic to allow only a single "Configurations" element, and use string.Empty as the item ref.
        modified |= this.ApplyModelToXmlGeneric<List<SolutionModel>, SolutionModel, XmlConfigurations>(
            modelCollection: modelSolution.IsConfigurationImplicit() ? [] : [modelSolution],
            ref this.configurationsSingle,
            Keyword.Configurations,
            getItemRefs: static (items) => items.ToList(static x => string.Empty),
            getModelItem: static (items, _) => items.FirstOrDefault(),
            applyModelToXml: static (newConfigs, newValue) => newConfigs.ApplyModelToXml(newValue));

        // Folders
        modified |= this.ApplyModelToXmlGeneric(
            modelCollection: modelSolution.SolutionFolders,
            ref this.folders,
            Keyword.Folder,
            getItemRefs: static (modelProjects, modelSolution) => modelProjects.ToList(x => x.ItemRef),
            getModelItem: static (modelProjects, itemRef, modelSolution) => ModelHelper.FindByItemRef(modelProjects, itemRef),
            applyModelToXml: static (newFolder, newValue, modelSolution) => newFolder.ApplyModelToXml(modelSolution, newValue),
            modelSolution);

        // Projects
        modified |= this.ApplyModelToXmlGeneric(
            modelCollection: modelSolution.SolutionProjects.ToList(x => (ItemRef: PathExtensions.ConvertToPersistencePath(x.ItemRef), Item: x)),
            ref this.Projects,
            Keyword.Project,
            getItemRefs: static (modelProjects) => modelProjects.WhereToList((x, _) => x.Item.Parent is null, (x, _) => x.ItemRef, false),
            getModelItem: static (modelProjects, itemRef) => ModelHelper.FindByItemRef(modelProjects, itemRef, x => x.ItemRef),
            applyModelToXml: static (newProject, newValue) => newProject.ApplyModelToXml(newValue.Item));

        // Properties
        modified |= this.ApplyModelToXml(modelSolution.Properties);

        return modified;
    }
}
