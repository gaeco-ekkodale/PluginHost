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
/// Interface for repository operations related to plugins.
/// Provides methods to manage plugin entities in the database.
/// </summary>
public interface IPluginRepository
{
    /// <summary>
    /// Adds a new plugin to the repository.
    /// </summary>
    /// <param name="plugin">The plugin entity to add</param>
    void AddPlugin(Plugin plugin);

    /// <summary>
    /// Updates an existing plugin.
    /// </summary>
    /// <param name="plugin">The plugin entity with updated values</param>
    void UpdatePlugin(Plugin plugin);

    /// <summary>
    /// Deletes a plugin by its ID.
    /// </summary>
    /// <param name="pluginId">The ID of the plugin to delete</param>
    void DeletePlugin(string pluginId);

    /// <summary>
    /// Retrieves a plugin by its ID.
    /// </summary>
    /// <param name="pluginId">The ID of the plugin to retrieve</param>
    /// <returns>The plugin entity if found, otherwise null</returns>
    Plugin? GetPluginById(string pluginId);

    /// <summary>
    /// Retrieves all plugins.
    /// </summary>
    /// <returns>A list of all plugins</returns>
    List<Plugin> GetAllPlugins();

    /// <summary>
    /// Reorder the plugin list by sorting the given indices to the beginning
    /// </summary>
    /// <param name="indices">The new ordered indices.</param>
    void ReorderPluginsByIndices(List<string> indices);

    /// <summary>
    /// Updates the order and group assignment for a set of plugins in a single batch.
    /// </summary>
    /// <param name="patches">Patch entries containing the new GroupId and Order for each PluginId.</param>
    void PatchPluginOrders(IEnumerable<PluginOrderPatch> patches);

    /// <summary>
    /// Applies a full plugin snapshot atomically.
    /// Missing plugins are deleted, existing plugins are updated, and new plugins are inserted.
    /// Concurrent snapshots are serialized, so the returned result reflects exactly what this
    /// call changed.
    /// </summary>
    /// <param name="plugins">The complete plugin list representing the desired final state.</param>
    /// <returns>The plugins that were inserted and removed by this call.</returns>
    PluginSnapshotResult ApplyPluginSnapshot(IEnumerable<Plugin> plugins);
}