// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

using PluginHost.Domain.Models;

namespace PluginHost.Domain.Repositories;

/// <summary>
/// Interface for repository operations related to PluginGroups.
/// Provides methods to manage pluginGroups in the database.
/// </summary>
public interface IPluginGroupRepository
{
    /// <summary>
    /// Adds a new pluginGroup to the repository.
    /// </summary>
    /// <param name="pluginGroup">The pluginGroup to add</param>
    void AddPluginGroup(PluginGroup pluginGroup);

    /// <summary>
    /// Retrieves a pluginGroup by its ID.
    /// </summary>
    /// <param name="groupId">The ID of the pluginGroup to retrieve</param>
    /// <returns>The pluginGroup if found, otherwise null</returns>
    PluginGroup? GetPluginGroupById(Guid groupId);

    /// <summary>
    /// Retrieves all pluginGroups
    /// </summary>
    /// <returns>All pluginGroups</returns>
    List<PluginGroup>? GetAllPluginGroups();

    /// <summary>
    /// Deletes a pluginGroup by its ID.
    /// </summary>
    /// <param name="groupId">The ID of the pluginGroup to delete</param>
    void DeletePluginGroup(Guid groupId);

    /// <summary>
    /// Updates the name and order of the given plugin groups.
    /// </summary>
    /// <param name="pluginGroups">The groups with updated values</param>
    void UpdatePluginGroups(IEnumerable<PluginGroup> pluginGroups);
}