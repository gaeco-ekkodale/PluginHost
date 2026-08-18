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
using PluginHost.Domain.Repositories;

namespace PluginHost.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for plugin group data access operations using PostgreSQL.
/// Handles CRUD operations for plugin groups.
/// </summary>
public class PluginGroupRepository : IPluginGroupRepository
{
    private readonly PluginHostDbContext _dbContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="PluginGroupRepository"/> class.
    /// </summary>
    /// <param name="dbContext">The database context</param>
    public PluginGroupRepository(PluginHostDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <inheritdoc />
    public void AddPluginGroup(PluginGroup pluginGroup)
    {
        var existingGroup = _dbContext.PluginGroups
            .FirstOrDefault(g => g.GroupId == pluginGroup.GroupId);

        if (existingGroup != null)
        {
            throw new Exception($"PluginGroup with ID {pluginGroup.GroupId} already exists");
        }

        _dbContext.PluginGroups.Add(pluginGroup);
        _dbContext.SaveChanges();
    }

    /// <inheritdoc />
    public void DeletePluginGroup(Guid groupId)
    {
        var pluginGroup = _dbContext.PluginGroups.FirstOrDefault(g => g.GroupId == groupId);
        if (pluginGroup == null) return;

        var pluginsInGroup = _dbContext.Plugins.Where(p => p.GroupId == groupId).ToList();
        if (pluginsInGroup.Count > 0)
        {
            var fallbackGroup = _dbContext.PluginGroups
                .Where(g => g.GroupId != groupId)
                .OrderBy(g => g.Order)
                .FirstOrDefault();

            if (fallbackGroup is null)
            {
                fallbackGroup = new PluginGroup { GroupId = Guid.NewGuid(), Name = "Neue Plugins", Order = 0 };
                _dbContext.PluginGroups.Add(fallbackGroup);
            }

            foreach (var plugin in pluginsInGroup)
                plugin.GroupId = fallbackGroup.GroupId;
        }

        _dbContext.PluginGroups.Remove(pluginGroup);
        _dbContext.SaveChanges();
    }

    /// <inheritdoc />
    public PluginGroup? GetPluginGroupById(Guid groupId)
    {
        return _dbContext.PluginGroups
            .FirstOrDefault(g => g.GroupId == groupId);
    }

    /// <inheritdoc />
    public List<PluginGroup>? GetAllPluginGroups()
    {
        return _dbContext.PluginGroups.ToList();
    }

    /// <inheritdoc />
    public void UpdatePluginGroups(IEnumerable<PluginGroup> pluginGroups)
    {
        foreach (var update in pluginGroups)
        {
            var existing = _dbContext.PluginGroups.FirstOrDefault(g => g.GroupId == update.GroupId);
            if (existing != null)
            {
                existing.Name = update.Name;
                existing.Order = update.Order;
            }
        }
        _dbContext.SaveChanges();
    }
}
