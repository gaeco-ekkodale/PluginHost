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

namespace PluginHost.Api.Endpoints.PluginGroups;

/// <summary>
/// Request payload for deleting a plugin group.
/// </summary>
public class DeletePluginGroupRequest
{
    /// <summary>The GUID of the group to delete.</summary>
    public Guid GroupId { get; set; }
}

/// <summary>
/// Deletes a plugin group.
/// Plugins that belong to the group will have their GroupId reset to <see cref="Guid.Empty"/>.
/// </summary>
public class DeletePluginGroup(IPluginGroupService pluginGroupService)
    : Endpoint<DeletePluginGroupRequest>
{
    public override void Configure()
    {
        Delete("plugin-groups/{groupId}");
        Summary(s =>
        {
            s.Summary = "Deletes the plugin group with the specified ID.";
            s.Response(200, "The group was deleted.");
            s.Response(401, "The caller is not authenticated.");
            s.Response(403, "The caller does not have the required admin permissions.");
        });
    }

    public override async Task HandleAsync(DeletePluginGroupRequest req, CancellationToken ct)
    {
        pluginGroupService.DeletePluginGroup(req.GroupId);
        await SendOkAsync(ct);
    }
}
