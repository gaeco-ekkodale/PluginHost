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
using FluentValidation;
using PluginHost.Api.Shared.DTOs;
using PluginHost.API.Services.Interfaces;
using PluginHost.Domain.Models;

namespace PluginHost.Api.Endpoints.PluginMenu;

/// <summary>Layout snapshot sent by the client to persist a full reorder/rename.</summary>
public class UpdatePluginLayoutRequest
{
    public List<PluginLayoutGroupEntry> Groups { get; set; } = [];

    /// <summary>IDs of groups the user explicitly deleted; these will be removed from the database.</summary>
    public List<Guid> DeletedGroupIds { get; set; } = [];
}

/// <summary>One group entry inside an <see cref="UpdatePluginLayoutRequest"/>.</summary>
public class PluginLayoutGroupEntry
{
    public Guid GroupId { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<string> Plugins { get; set; } = [];
}

public class UpdatePluginLayoutValidator : Validator<UpdatePluginLayoutRequest>
{
    public UpdatePluginLayoutValidator()
    {
        RuleFor(x => x.Groups)
            .NotNull().WithMessage("Groups must not be null.");

        RuleForEach(x => x.Groups).ChildRules(group =>
        {
            group.RuleFor(g => g.GroupId)
                .NotEqual(Guid.Empty).WithMessage("GroupId must be a valid non-empty GUID.");
            group.RuleFor(g => g.Name)
                .NotEmpty().WithMessage("Group name must not be empty.")
                .MaximumLength(200);
            group.RuleForEach(g => g.Plugins)
                .NotEmpty().WithMessage("Plugin IDs must not be empty.")
                .MaximumLength(200);
        });
    }
}

/// <summary>
/// Persists a full layout snapshot in one request.
/// The caller sends the complete desired state – group names and order, plus each
/// plugin's group assignment and order.  Groups not present in the payload are left
/// unchanged.  Plugins not listed in any group are also left unchanged.
/// </summary>
public class UpdatePluginLayout(
    IPluginGroupService pluginGroupService,
    IPluginService pluginService)
    : Endpoint<UpdatePluginLayoutRequest>
{
    public override void Configure()
    {
        Put("plugin-menu");
        Summary(s =>
        {
            s.Summary = "Saves a full plugin layout snapshot.";
            s.Description = "Updates group names and display order, and sets each plugin's group assignment and order in a single atomic call. Groups and plugins not included in the payload are left untouched.";
            s.Response(200, "Layout was saved.");
            s.Response(400, "The payload is invalid.");
            s.Response(401, "The caller is not authenticated.");
            s.Response(403, "The caller does not have the required admin permissions.");
            s.Response(404, "One or more plugin IDs were not found.");
        });
    }

    public override async Task HandleAsync(UpdatePluginLayoutRequest req, CancellationToken ct)
    {
        var existingIds = (pluginGroupService.GetAllPluginGroups() ?? [])
            .Select(g => g.GroupId)
            .ToHashSet();

        // 1. Create groups that do not yet exist in the database
        foreach (var (g, i) in req.Groups.Select((g, i) => (g, i)).Where(x => !existingIds.Contains(x.g.GroupId)))
        {
            pluginGroupService.AddPluginGroup(new PluginGroup
            {
                GroupId = g.GroupId,
                Name = g.Name,
                Order = i
            });
        }

        // 2. Update groups that already exist — name and order derived from payload position
        var groupUpdates = req.Groups
            .Select((g, i) => (g, i))
            .Where(x => existingIds.Contains(x.g.GroupId))
            .Select(x => new PluginGroup { GroupId = x.g.GroupId, Name = x.g.Name, Order = x.i });
        pluginGroupService.UpdatePluginGroups(groupUpdates);

        // 3. Update plugin group assignments before deleting groups to avoid FK violations
        var pluginPatches = req.Groups.SelectMany(g =>
            g.Plugins.Select((p, i) => new PluginOrderPatch(p, g.GroupId, i)));

        try
        {
            await pluginService.PatchPluginOrdersAsync(pluginPatches);
        }
        catch (ArgumentException ex)
        {
            ThrowError(ex.Message, 404);
        }

        // 4. Delete groups not included in the request, with no plugins, or explicitly marked for deletion
        var groupsWithPlugins = req.Groups
            .Where(g => g.Plugins.Count > 0)
            .Select(g => g.GroupId)
            .ToHashSet();

        var obsoleteGroupIds = (pluginGroupService.GetAllPluginGroups() ?? [])
            .Select(g => g.GroupId)
            .Where(id => !groupsWithPlugins.Contains(id))
            .Union(req.DeletedGroupIds)
            .ToList();

        foreach (var groupId in obsoleteGroupIds)
            pluginGroupService.DeletePluginGroup(groupId);

        await pluginService.PublishPluginChangeAsync(new PluginChangeEvent
        {
            ChangeType = "menu",
            Operation = "MenuLayoutChanged",
            Message = "Das App-Menü wurde aktualisiert.",
            Source = "menu-layout",
            TotalGroups = req.Groups.Count
        });

        await SendOkAsync(ct);
    }
}
