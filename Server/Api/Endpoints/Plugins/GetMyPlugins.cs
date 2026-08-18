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
/// Returns only the plugins for which the authenticated user holds a license,
/// determined by the "aud" claims in the JWT token.
/// </summary>
public class GetMyPlugins(IPluginRepository pluginRepository, PluginMapper pluginMapper)
    : EndpointWithoutRequest<IEnumerable<GetPluginDto>>
{
    public override void Configure()
    {
        Get("plugins/my-plugins");
        // Override the global AdminOnly policy – only authentication is required.
        Policies();
        Summary(s =>
        {
            s.Summary = "Retrieves all plugins that the current user has access to.";
            s.Response<IEnumerable<GetPluginDto>>(200, "Returns the list of licensed plugins or an empty collection.");
            s.Response(401, "The caller is not authenticated.");
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var licenses = GetUserLicenses();

        if (licenses.Count == 0)
        {
            await SendOkAsync(Enumerable.Empty<GetPluginDto>(), ct);
            return;
        }

        var plugins = pluginRepository.GetAllPlugins()
            .Where(p => licenses.Contains(p.Id))
            .ToList();

        if (plugins.Count == 0)
        {
            await SendOkAsync(Enumerable.Empty<GetPluginDto>(), ct);
            return;
        }

        var dtos = plugins.Select(p => pluginMapper.FromEntity(p));
        await SendOkAsync(dtos, ct);
    }

    private List<string> GetUserLicenses()
    {
        var user = HttpContext.User;
        if (user.Identity == null || !user.Identity.IsAuthenticated)
            return [];

        return [.. user.FindAll("aud").Select(c => c.Value)];
    }
}
