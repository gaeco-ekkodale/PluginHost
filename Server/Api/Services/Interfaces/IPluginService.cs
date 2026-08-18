// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

using PluginHost.Api.Shared.DTOs;
using PluginHost.Domain.Models;

namespace PluginHost.API.Services.Interfaces;

/// <summary>
/// Interface for plugin management operations.
/// Provides methods to add, delete, and retrieve plugin files.
/// </summary>
public interface IPluginService
{
    /// <summary>
    /// Registers a new container plugin with metadata only.
    /// </summary>
    /// <param name="plugin">The plugin entity to register</param>
    /// <returns>Task representing the asynchronous operation</returns>
    Task RegisterPluginAsync(Plugin plugin);

    /// <summary>
    /// Deletes a plugin and all associated files.
    /// </summary>
    /// <param name="pluginId">The ID of the plugin to be deleted</param>
    /// <returns>Task representing the asynchronous operation</returns>
    Task DeletePluginAsync(string pluginId);

    Task EnablePluginAsync(string pluginId);

    Task DisablePluginAsync(string pluginId);

    /// <summary>
    /// Retrieves a file of a plugin.
    /// </summary>
    /// <param name="token">The JWT token for accessing the file</param>
    /// <param name="filename">The name of the file</param>
    /// <returns>A stream with the file and the content type</returns>
    Task<(Stream Stream, string ContentType)> GetPluginFileAsync(string token, string filename);

    /// <summary>
    /// Exchanges a JWT token for a plugin ID.
    /// </summary>
    Task<TokenResponse> ExchangeToken(string accessToken, string targetClientId);

    /// <summary>
    /// Updates the display order and group assignment for a set of plugins in one batch.
    /// </summary>
    /// <param name="patches">Per-plugin order and group changes.</param>
    Task PatchPluginOrdersAsync(IEnumerable<PluginOrderPatch> patches);

    /// <summary>
    /// Publishes a structured plugin change event to all SignalR clients.
    /// </summary>
    /// <param name="changeEvent">The change payload.</param>
    Task PublishPluginChangeAsync(PluginChangeEvent changeEvent);
}