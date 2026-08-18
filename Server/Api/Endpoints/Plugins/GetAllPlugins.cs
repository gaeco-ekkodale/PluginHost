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
using PluginHost.Api.Shared.DTOs;
using PluginHost.Api.Shared.Mappers;
using PluginHost.Domain.Repositories;

namespace PluginHost.Api.Endpoints.Plugins;

/// <summary>
/// Returns all registered plugins including their signed icon URLs.
/// The remote-entry URL is intentionally omitted from this admin listing.
/// </summary>
public class GetAllPlugins(IPluginRepository pluginRepository, PluginMapper pluginMapper)
    : EndpointWithoutRequest<IEnumerable<GetPluginDto>>
{
    public override void Configure()
    {
        Get("plugins");
        // Override the global AdminOnly policy – only authentication is required.
        Policies();
        Summary(s =>
        {
            s.Summary = "Retrieves the plugin index.";
            s.Response<IEnumerable<GetPluginDto>>(200, "Returns the list of plugins or an empty collection if none are found.");
            s.Response(401, "The caller is not authenticated.");
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var plugins = pluginRepository.GetAllPlugins();

        if (plugins.Count == 0)
        {
            await SendOkAsync(Enumerable.Empty<GetPluginDto>(), ct);
            return;
        }

        var dtos = plugins.Select(p => pluginMapper.FromEntityWithoutRemoteEntry(p));
        await SendOkAsync(dtos, ct);
    }
}
