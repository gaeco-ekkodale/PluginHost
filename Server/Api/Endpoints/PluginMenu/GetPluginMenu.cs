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
using PluginHost.API.Services.Interfaces;
using PluginHost.Domain.Repositories;

namespace PluginHost.Api.Endpoints.PluginMenu;

public class PluginMenuGroupDto
{
    public required Guid GroupId { get; set; }
    public required string Name { get; set; }
    public List<GetPluginDto> Plugins { get; set; } = [];
}

/// <summary>
/// Returns the full plugin menu: groups ordered by their <c>Order</c> field,
/// each containing their assigned plugins also sorted by <c>Order</c>.
/// Plugin URLs (remote entry + icon) are signed and ready for the client to consume.
/// </summary>
public class GetPluginMenu(
    IPluginGroupService pluginGroupService,
    IPluginRepository pluginRepository,
    PluginMapper pluginMapper)
    : EndpointWithoutRequest<IEnumerable<PluginMenuGroupDto>>
{
    public override void Configure()
    {
        Get("plugin-menu");
        // Only authentication required – every logged-in user needs the menu.
        Policies();
        Summary(s =>
        {
            s.Summary = "Returns the full plugin navigation menu.";
            s.Description = "Groups are sorted by Order; plugins within each group are also sorted by Order. Plugin URLs are pre-signed and can be used directly by the microfrontend shell.";
            s.Response<IEnumerable<PluginMenuGroupDto>>(200, "The ordered menu tree was returned.");
            s.Response(401, "The caller is not authenticated.");
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var licenses = GetUserLicenses();

        var groups = pluginGroupService.GetAllPluginGroups() ?? [];
        var plugins = pluginRepository.GetAllPlugins()
            .Where(p => licenses.Contains(p.Id))
            .ToList();

        // Build a lookup: group -> plugins sorted by their persisted Order (= original array position)
        var pluginsByGroup = plugins
            .GroupBy(p => p.GroupId)
            .ToDictionary(g => g.Key, g => g.OrderBy(p => p.Order).ToList());

        var menu = groups
            .OrderBy(g => g.Order)
            .Select(group => new PluginMenuGroupDto
            {
                GroupId = group.GroupId,
                Name = group.Name,
                Plugins = pluginsByGroup.TryGetValue(group.GroupId, out var groupPlugins)
                    ? [.. groupPlugins.Select(p => pluginMapper.FromEntity(p))]
                    : []
            });

        await SendOkAsync(menu, ct);
    }

    private List<string> GetUserLicenses()
    {
        var user = HttpContext.User;
        if (user.Identity == null || !user.Identity.IsAuthenticated)
            return [];

        return [.. user.FindAll("aud").Select(c => c.Value)];
    }
}
