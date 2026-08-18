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
using System.ComponentModel.DataAnnotations;

namespace PluginHost.Api.Endpoints.PluginGroups;

public class GetPluginGroupDto
{
    [Required]
    public required Guid GroupId { get; set; }

    [Required]
    public required string Name { get; set; }
}

/// <summary>
/// Returns all plugin groups ordered by their Order field.
/// </summary>
public class GetAllPluginGroups(IPluginGroupService pluginGroupService)
    : EndpointWithoutRequest<IEnumerable<GetPluginGroupDto>>
{
    public override void Configure()
    {
        Get("plugin-groups");
        Summary(s =>
        {
            s.Summary = "Fetches all plugin groups.";
            s.Response<IEnumerable<GetPluginGroupDto>>(200, "All groups were returned.");
            s.Response(401, "The caller is not authenticated.");
            s.Response(403, "The caller does not have the required admin permissions.");
            s.Response(404, "No groups were found.");
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var groups = pluginGroupService.GetAllPluginGroups();

        if (groups == null || groups.Count == 0)
        {
            await SendNotFoundAsync(ct);
            return;
        }

        await SendOkAsync(groups.OrderBy(g => g.Order).Select(g => new GetPluginGroupDto
        {
            GroupId = g.GroupId,
            Name = g.Name
        }), ct);
    }
}
