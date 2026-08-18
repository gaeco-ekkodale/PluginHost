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

namespace PluginHost.API.Services.Interfaces;

/// <summary>
/// Interface for plugin group management operations.
/// </summary>
public interface IPluginGroupService
{
    /// <summary>
    /// Adds a new plugin group.
    /// </summary>
    /// <param name="pluginGroup">The plugin group to add</param>
    void AddPluginGroup(PluginGroup pluginGroup);

    /// <summary>
    /// Deletes a plugin group by its ID.
    /// </summary>
    /// <param name="groupId">The ID of the group to delete</param>
    void DeletePluginGroup(Guid groupId);

    /// <summary>
    /// Retrieves all plugin groups.
    /// </summary>
    /// <returns>All plugin groups, or null if none exist</returns>
    List<PluginGroup>? GetAllPluginGroups();

    /// <summary>
    /// Updates the name and order of the given plugin groups.
    /// </summary>
    /// <param name="pluginGroups">The groups with updated values</param>
    void UpdatePluginGroups(IEnumerable<PluginGroup> pluginGroups);
}
