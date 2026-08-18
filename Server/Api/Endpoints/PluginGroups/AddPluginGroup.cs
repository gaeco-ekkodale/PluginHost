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
using PluginHost.API.Services.Interfaces;
using PluginHost.Domain.Models;

namespace PluginHost.Api.Endpoints.PluginGroups;

/// <summary>
/// Request payload for creating a new plugin group.
/// </summary>
public class AddPluginGroupRequest
{
    /// <summary>Human-readable name shown in the UI.</summary>
    public string Name { get; set; } = string.Empty;
}

public class AddPluginGroupValidator : Validator<AddPluginGroupRequest>
{
    public AddPluginGroupValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(200);
    }
}

/// <summary>
/// Creates a new plugin group.
/// </summary>
public class AddPluginGroup(IPluginGroupService pluginGroupService)
    : Endpoint<AddPluginGroupRequest>
{
    public override void Configure()
    {
        Post("plugin-groups");
        Summary(s =>
        {
            s.Summary = "Creates a new plugin group.";
            s.Response(200, "The group was created.");
            s.Response(400, "The request payload is invalid.");
            s.Response(401, "The caller is not authenticated.");
            s.Response(403, "The caller does not have the required admin permissions.");
        });
    }

    public override async Task HandleAsync(AddPluginGroupRequest req, CancellationToken ct)
    {
        pluginGroupService.AddPluginGroup(new PluginGroup
        {
            GroupId = Guid.NewGuid(),
            Name = req.Name
        });

        await SendOkAsync(ct);
    }
}
