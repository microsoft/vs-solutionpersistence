// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.SolutionPersistence.Model;

/// <summary>
/// Allows the serializer to extend the model with properties that are specific to the serializer.
/// </summary>
public interface ISerializerModelExtension
{
    /// <summary>
    /// Gets the serializer that is extending the model.
    /// </summary>
    public ISolutionSerializer Serializer { get; }
}

/// <summary>
/// Allows the serializer to extend the model with properties that are specific to the serializer.
/// </summary>
/// <typeparam name="TSettings">The settings type for the serializer.</typeparam>
public interface ISerializerModelExtension<TSettings> : ISerializerModelExtension
{
    /// <summary>
    /// Gets the settings that are specific to the serializer.
    /// </summary>
    public TSettings Settings { get; }
}
