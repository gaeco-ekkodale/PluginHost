// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

using FastEndpoints;
using PluginHost.API.Services.Interfaces;
using PluginHost.Domain.Repositories;

namespace PluginHost.Api.Endpoints.Plugins;

/// <summary>
/// Request payload for deleting a plugin.
/// </summary>
public class DeletePluginRequest
{
    /// <summary>The unique ID of the plugin to delete.</summary>
    public string PluginId { get; set; } = string.Empty;
}

/// <summary>
/// Deletes a plugin and all its associated files from the system.
/// </summary>
public class DeletePlugin(
    IPluginRepository pluginRepository,
    IPluginService pluginService)
    : Endpoint<DeletePluginRequest>
{
    public override void Configure()
    {
        Delete("plugins/{pluginId}");
        Summary(s =>
        {
            s.Summary = "Deletes a plugin from the system.";
            s.Description = "Removes the plugin registration and all associated container files. This operation is irreversible.";
            s.Response(200, "The plugin was successfully deleted.");
            s.Response(401, "The caller is not authenticated.");
            s.Response(403, "The caller does not have the required admin permissions.");
            s.Response(404, "No plugin with the specified ID was found.");
        });
    }

    public override async Task HandleAsync(DeletePluginRequest req, CancellationToken ct)
    {
        var plugin = pluginRepository.GetPluginById(req.PluginId);
        if (plugin is null)
        {
            await SendNotFoundAsync(ct);
            return;
        }

        await pluginService.DeletePluginAsync(req.PluginId);

        await SendOkAsync(ct);
    }
}
