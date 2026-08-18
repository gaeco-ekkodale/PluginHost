// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using PluginHost.Api.Core.Options;
using PluginHost.Api.Shared.DTOs;
using PluginHost.API.Hubs;
using PluginHost.API.Services.Interfaces;
using PluginHost.API.Services.Interfaces.Keycloak;
using PluginHost.Domain.Models;
using PluginHost.Domain.Repositories;

namespace PluginHost.API.Services;

/// <summary>
/// Implementation of the IPluginService interface.
/// Manages plugin operations including adding, deleting, and retrieving plugin files.
/// </summary>
public class PluginService : IPluginService
{
    private readonly IPluginRepository _pluginRepository;
    private readonly IPluginGroupService _pluginGroupService;
    private readonly IKeycloakClientManager _keycloakClientManager;
    private readonly ISignedUrlService _signedUrlService;
    private readonly ILogger<PluginService> _logger;
    private readonly IHubContext<DataHub> _hubContext;
    private readonly SignalROptions _signalROptions;
    private readonly HttpClient _httpClient;

    /// <summary>
    /// Initializes a new instance of the PluginService class.
    /// </summary>
    /// <param name="pluginRepository">The plugin repository</param>
    /// <param name="keycloakHelper">The Keycloak helper service</param>
    /// <param name="signedUrlService">The signed URL service</param>
    /// <param name="logger">The logger</param>
    /// <param name="signalROptions">The options for SignalR defining messaging.</param>
    public PluginService(
        IPluginRepository pluginRepository,
        IPluginGroupService pluginGroupService,
        IKeycloakHelper keycloakHelper,
        IKeycloakClientManager keycloakClientManager,
        ISignedUrlService signedUrlService,
        ILogger<PluginService> logger,
        IHubContext<DataHub> hubContext,
        IOptions<SignalROptions> signalROptions,
        IHttpClientFactory httpClientFactory)
    {
        _pluginRepository = pluginRepository;
        _pluginGroupService = pluginGroupService;
        _keycloakClientManager = keycloakClientManager;
        _signedUrlService = signedUrlService;
        _logger = logger;
        _hubContext = hubContext;
        _signalROptions = signalROptions.Value;
        _httpClient = httpClientFactory.CreateClient();
    }

    /// <inheritdoc />
    public async Task RegisterPluginAsync(Plugin plugin)
    {
        try
        {
            // Data is already set via AutoMapper, just validate
            if (string.IsNullOrEmpty(plugin.ContainerUrl))
            {
                throw new ArgumentException("ContainerUrl is required for container plugins");
            }

            if (string.IsNullOrEmpty(plugin.EntrypointPath))
            {
                throw new ArgumentException("EntrypointPath is required for container plugins");
            }

            plugin.GroupId = ResolveDefaultGroup();

            // Add client role to the Admin group
            var success = await _keycloakClientManager.CreateClientWithTokenExchange(plugin.Id);

            // Add plugin to the database
            _pluginRepository.AddPlugin(plugin);

            await PublishPluginChangeAsync(new PluginChangeEvent
            {
                ChangeType = "catalog",
                Operation = _signalROptions.Operation.AddPlugin,
                Message = $"Plugin '{plugin.DisplayName}' wurde hinzugefügt.",
                Source = "create",
                RequiresTokenRefresh = true,
                AddedPluginIds = [plugin.Id],
                AddedPlugins =
                [
                    new PluginChangeItem
                    {
                        Id = plugin.Id,
                        Name = plugin.DisplayName,
                        Route = plugin.Route
                    }
                ],
                TotalPlugins = (_pluginRepository.GetAllPlugins() ?? []).Count
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering container plugin {PluginId}", plugin.Id);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task DeletePluginAsync(string pluginId)
    {
        try
        {
            var plugin = _pluginRepository.GetPluginById(pluginId) ?? throw new FileNotFoundException($"Plugin with ID {pluginId} not found");

            // Delete plugin from the database
            _pluginRepository.DeletePlugin(pluginId);

            await PublishPluginChangeAsync(new PluginChangeEvent
            {
                ChangeType = "catalog",
                Operation = _signalROptions.Operation.DeletePlugin,
                Message = $"Plugin '{plugin.DisplayName}' wurde entfernt.",
                Source = "delete",
                RequiresTokenRefresh = true,
                RemovedPluginIds = [pluginId],
                RemovedPlugins =
                [
                    new PluginChangeItem
                    {
                        Id = plugin.Id,
                        Name = plugin.DisplayName,
                        Route = plugin.Route
                    }
                ],
                TotalPlugins = (_pluginRepository.GetAllPlugins() ?? []).Count
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting plugin {PluginId}", pluginId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<(Stream Stream, string ContentType)> GetPluginFileAsync(string token, string filename)
    {
        try
        {
            var pluginToken = _signedUrlService.ValidateJwtToken(token)
                ?? throw new SecurityTokenException("Invalid JWT token.");

            if (!string.IsNullOrEmpty(pluginToken.Filename) &&
                !string.Equals(pluginToken.Filename, filename, StringComparison.OrdinalIgnoreCase))
            {
                throw new UnauthorizedAccessException($"Token is not valid for file {filename}");
            }

            var plugin = _pluginRepository.GetPluginById(pluginToken.PluginId)
                ?? throw new FileNotFoundException($"Plugin with ID {pluginToken.PluginId} not found");

            // Build URL: ensure we handle slashes correctly
            var baseUrl = plugin.ContainerUrl.TrimEnd('/');
            var relativePath = filename.TrimStart('/');
            var url = $"{baseUrl}/{relativePath}";

            _logger.LogInformation("Proxying request to: {Url}", url);

            var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                throw new FileNotFoundException($"File {filename} not found at {url}");
            }

            response.EnsureSuccessStatusCode();

            var contentType = response.Content.Headers.ContentType?.ToString() ?? "application/octet-stream";
            var stream = await response.Content.ReadAsStreamAsync();

            return (stream, contentType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error serving plugin file {Filename}", filename);
            throw;
        }
    }

    /// <summary>
    /// Generates a token for plugin access.
    /// </summary>
    /// <param name="pluginId">The plugin ID to generate a token for</param>
    /// <returns>A JWT token for accessing the plugin</returns>
    public string GeneratePluginToken(string pluginId)
    {
        return _signedUrlService.GenerateToken(new PluginToken { PluginId = pluginId });
    }

    public async Task<TokenResponse> ExchangeToken(string accessToken, string targetClientId)
    {
        return await _keycloakClientManager.ExchangeToken(accessToken, targetClientId);
    }

    public Task EnablePluginAsync(string pluginId)
    {
        throw new NotImplementedException();
    }

    public Task DisablePluginAsync(string pluginId)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    /// <summary>
    /// Returns the GroupId of the first empty group, or creates a new "Neue Plugins" group.
    /// </summary>
    private Guid ResolveDefaultGroup()
    {
        var allGroups = _pluginGroupService.GetAllPluginGroups() ?? [];
        var usedGroupIds = (_pluginRepository.GetAllPlugins() ?? [])
            .Select(p => p.GroupId)
            .ToHashSet();

        var emptyGroup = allGroups.FirstOrDefault(g => !usedGroupIds.Contains(g.GroupId));
        if (emptyGroup is not null)
            return emptyGroup.GroupId;

        var newGroup = new PluginGroup
        {
            GroupId = Guid.NewGuid(),
            Name = "Neue Plugins",
            Order = allGroups.Count
        };
        _pluginGroupService.AddPluginGroup(newGroup);
        return newGroup.GroupId;
    }

    public async Task PatchPluginOrdersAsync(IEnumerable<PluginOrderPatch> patches)
    {
        try
        {
            _pluginRepository.PatchPluginOrders(patches);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error patching plugin orders");
            throw;
        }
    }

    public async Task PublishPluginChangeAsync(PluginChangeEvent changeEvent)
    {
        await _hubContext.Clients.All.SendAsync(_signalROptions.PluginEvent, changeEvent);
    }
}