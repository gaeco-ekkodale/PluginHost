// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

using Microsoft.EntityFrameworkCore;
using PluginHost.Domain.Models;
using PluginHost.Domain.Repositories;

namespace PluginHost.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for plugin data access operations using PostgreSQL.
/// Handles CRUD operations for plugin entities.
/// </summary>
public class PluginRepository : IPluginRepository
{
    /// <summary>
    /// Arbitrary but stable key identifying the PostgreSQL advisory lock that serializes
    /// snapshot application. Every process applying a snapshot must use the same value.
    /// </summary>
    private const long PluginSnapshotLockKey = 8_248_301_552_004_119_001;

    private readonly PluginHostDbContext _dbContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="PluginRepository"/> class.
    /// </summary>
    /// <param name="dbContext">The database context</param>
    public PluginRepository(PluginHostDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <inheritdoc />
    public void AddPlugin(Plugin plugin)
    {
        var existingPlugin = _dbContext.Plugins
            .FirstOrDefault(p => p.Id == plugin.Id);

        if (existingPlugin != null)
        {
            throw new InvalidOperationException($"Plugin with ID {plugin.Id} already exists");
        }
        // throw error if route is already used by another plugin
        if (_dbContext.Plugins.Any(p => p.Route == plugin.Route))
        {
            throw new InvalidOperationException($"Plugin route '{plugin.Route}' is already used by another plugin");
        }

        _dbContext.Plugins.Add(plugin);
        _dbContext.SaveChanges();
    }

    /// <inheritdoc />
    public void UpdatePlugin(Plugin plugin)
    {
        var existingPlugin = _dbContext.Plugins
            .FirstOrDefault(p => p.Id == plugin.Id);

        if (existingPlugin is null)
        {
            throw new InvalidOperationException($"Plugin with ID {plugin.Id} does not exist");
        }
        // throw error if route is already used by another plugin
        if (_dbContext.Plugins.Any(p => p.Route == plugin.Route && p.Id != plugin.Id))
        {
            throw new InvalidOperationException($"Plugin route '{plugin.Route}' is already used by another plugin");
        }

        existingPlugin.DisplayName = plugin.DisplayName;
        existingPlugin.Description = plugin.Description;
        existingPlugin.IconPath = plugin.IconPath;
        existingPlugin.EntrypointPath = plugin.EntrypointPath;
        existingPlugin.ExposedModule = plugin.ExposedModule;
        existingPlugin.Route = plugin.Route;
        existingPlugin.ContainerUrl = plugin.ContainerUrl;
        _dbContext.SaveChanges();
    }

    /// <inheritdoc />
    public void DeletePlugin(string id)
    {
        var plugin = _dbContext.Plugins.FirstOrDefault(p => p.Id == id);
        if (plugin != null)
        {
            _dbContext.Plugins.Remove(plugin);
            _dbContext.SaveChanges();
        }
    }

    /// <inheritdoc />
    public List<Plugin> GetAllPlugins()
    {
        return _dbContext.Plugins.ToList();
    }

    /// <inheritdoc />
    public Plugin? GetPluginById(string id)
    {
        return _dbContext.Plugins
            .FirstOrDefault(p => p.Id == id);
    }

    /// <inheritdoc />
    public void PatchPluginOrders(IEnumerable<PluginOrderPatch> patches)
    {
        foreach (var patch in patches)
        {
            var plugin = _dbContext.Plugins.FirstOrDefault(p => p.Id == patch.PluginId);
            if (plugin is null)
                throw new ArgumentException($"Plugin with ID '{patch.PluginId}' not found.");

            plugin.GroupId = patch.GroupId;
            plugin.Order = patch.Order;
        }
        _dbContext.SaveChanges();
    }

    /// <inheritdoc />
    public void ReorderPluginsByIndices(List<string> indices)
    {
        // Note: Since PostgreSQL doesn't have built-in ordering by list position,
        // we can use a workaround with an Order column or handle ordering on the client side.
        // For now, this method validates that all plugins are present but doesn't enforce ordering in the database.

        var allPluginIds = _dbContext.Plugins.Select(p => p.Id).ToList();

        // Validate that all provided indices exist
        foreach (var index in indices)
        {
            if (!allPluginIds.Contains(index))
            {
                throw new ArgumentException($"Plugin with ID {index} not found");
            }
        }

        // In a real implementation, you might want to add an Order/Position column to the PluginEntity table
        // and update it here. For now, we'll just validate the indices.
    }

    /// <inheritdoc />
    public PluginSnapshotResult ApplyPluginSnapshot(IEnumerable<Plugin> plugins)
    {
        var snapshot = plugins.ToList();

        var duplicateIds = snapshot
            .GroupBy(p => p.Id, StringComparer.Ordinal)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        if (duplicateIds.Count > 0)
        {
            throw new InvalidOperationException($"Snapshot contains duplicate plugin ids: {string.Join(", ", duplicateIds)}");
        }

        var duplicateRoutes = snapshot
            .GroupBy(p => p.Route, StringComparer.Ordinal)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        if (duplicateRoutes.Count > 0)
        {
            throw new InvalidOperationException($"Snapshot contains duplicate routes: {string.Join(", ", duplicateRoutes)}");
        }

        using var transaction = _dbContext.Database.BeginTransaction();

        // Applying a snapshot is a read-then-write operation: the current plugin set decides
        // which entries are inserted, updated or deleted. Under READ COMMITTED two concurrent
        // snapshots would both read "plugin X missing" and both INSERT it, so the second one
        // fails with 23505 on PK_Plugins. The advisory lock serializes snapshot application
        // across all connections and is released automatically when the transaction ends.
        if (_dbContext.Database.IsNpgsql())
        {
            _dbContext.Database.ExecuteSqlRaw("SELECT pg_advisory_xact_lock({0})", PluginSnapshotLockKey);
        }

        // Read the current state only after the lock is held; anything loaded before it is stale.
        _dbContext.ChangeTracker.Clear();

        var existingPlugins = _dbContext.Plugins.ToDictionary(p => p.Id, StringComparer.Ordinal);
        var snapshotById = snapshot.ToDictionary(p => p.Id, StringComparer.Ordinal);

        var pluginsToDelete = existingPlugins.Values
            .Where(existing => !snapshotById.ContainsKey(existing.Id))
            .ToList();

        if (pluginsToDelete.Count > 0)
        {
            _dbContext.Plugins.RemoveRange(pluginsToDelete);
        }

        var allGroups = _dbContext.PluginGroups
            .OrderBy(g => g.Order)
            .ToList();

        var newPluginsGroup = allGroups.FirstOrDefault(g =>
            string.Equals(g.Name, "Neue Plugins", StringComparison.Ordinal));

        if (newPluginsGroup is null)
        {
            newPluginsGroup = new PluginGroup
            {
                GroupId = Guid.NewGuid(),
                Name = "Neue Plugins",
                Order = allGroups.Count
            };

            _dbContext.PluginGroups.Add(newPluginsGroup);
            allGroups.Add(newPluginsGroup);
        }

        var nextOrder = _dbContext.Plugins.Any()
            ? _dbContext.Plugins.Max(p => p.Order) + 1
            : 0;

        var addedPluginIds = new List<string>();

        foreach (var snapshotPlugin in snapshot)
        {
            if (existingPlugins.TryGetValue(snapshotPlugin.Id, out var existing))
            {
                existing.DisplayName = snapshotPlugin.DisplayName;
                existing.Description = snapshotPlugin.Description;
                existing.IconPath = snapshotPlugin.IconPath;
                existing.EntrypointPath = snapshotPlugin.EntrypointPath;
                existing.ExposedModule = snapshotPlugin.ExposedModule;
                existing.Route = snapshotPlugin.Route;
                existing.ContainerUrl = snapshotPlugin.ContainerUrl;
                continue;
            }

            snapshotPlugin.GroupId = newPluginsGroup.GroupId;
            snapshotPlugin.Order = nextOrder++;
            _dbContext.Plugins.Add(snapshotPlugin);
            addedPluginIds.Add(snapshotPlugin.Id);
        }

        _dbContext.SaveChanges();
        transaction.Commit();

        return new PluginSnapshotResult(addedPluginIds, pluginsToDelete);
    }
}
