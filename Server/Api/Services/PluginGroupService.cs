// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

using PluginHost.API.Services.Interfaces;
using PluginHost.Domain.Models;
using PluginHost.Domain.Repositories;

namespace PluginHost.API.Services;

/// <summary>
/// Implementation of the <see cref="IPluginGroupService"/> interface.
/// Manages plugin group operations.
/// </summary>
public class PluginGroupService : IPluginGroupService
{
    private readonly IPluginGroupRepository _pluginGroupRepository;
    private readonly ILogger<PluginGroupService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="PluginGroupService"/> class.
    /// </summary>
    /// <param name="pluginGroupRepository">The plugin group repository</param>
    /// <param name="logger">The logger</param>
    public PluginGroupService(IPluginGroupRepository pluginGroupRepository, ILogger<PluginGroupService> logger)
    {
        _pluginGroupRepository = pluginGroupRepository;
        _logger = logger;
    }

    /// <inheritdoc />
    public void AddPluginGroup(PluginGroup pluginGroup)
    {
        try
        {
            _pluginGroupRepository.AddPluginGroup(pluginGroup);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding plugin group {GroupId}", pluginGroup.GroupId);
            throw;
        }
    }

    /// <inheritdoc />
    public void DeletePluginGroup(Guid groupId)
    {
        try
        {
            _pluginGroupRepository.DeletePluginGroup(groupId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting plugin group {GroupId}", groupId);
            throw;
        }
    }

    /// <inheritdoc />
    public List<PluginGroup>? GetAllPluginGroups()
    {
        try
        {
            return _pluginGroupRepository.GetAllPluginGroups();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all plugin groups");
            throw;
        }
    }

    /// <inheritdoc />
    public void UpdatePluginGroups(IEnumerable<PluginGroup> pluginGroups)
    {
        try
        {
            _pluginGroupRepository.UpdatePluginGroups(pluginGroups);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating plugin groups");
            throw;
        }
    }
}
