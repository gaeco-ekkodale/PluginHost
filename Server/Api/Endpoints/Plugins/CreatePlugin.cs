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

namespace PluginHost.Api.Endpoints.Plugins;

/// <summary>
/// Request payload for registering a new container plugin.
/// </summary>
public class CreatePluginRequest
{
    /// <summary>Human-readable display name shown in the UI.</summary>
    public required string Id { get; set; }
    public required string DisplayName { get; set; }

    public string? Description { get; set; }

    /// <summary>Base URL of the container hosting the plugin, e.g. "http://my-plugin:8080".</summary>
    public required string ContainerBaseUrl { get; set; }

    /// <summary>Relative path to the plugin icon, e.g. "assets/icon.png".</summary>
    public string? IconPath { get; set; }

    /// <summary>Relative path to the module federation entry point, e.g. "assets/remoteEntry.js".</summary>
    public required string EntrypointPath { get; set; }

    /// <summary>Module name exposed by the plugin's module federation config.</summary>
    public required string ExposedModule { get; set; }

    /// <summary>Frontend route under which the plugin is mounted, e.g. "/my-plugin".</summary>
    public required string Route { get; set; }




}

public class RegisterContainerPluginValidator : Validator<CreatePluginRequest>
{
    public RegisterContainerPluginValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Id is required.")
            .Matches(@"^[a-z0-9\-]+$").WithMessage("Id must be lowercase alphanumeric with dashes only.");
        RuleFor(x => x.DisplayName)
            .NotEmpty().WithMessage("DisplayName is required.");
        RuleFor(x => x.ExposedModule)
            .NotEmpty().WithMessage("ExposedModule is required.");
        RuleFor(x => x.Route)
            .NotEmpty().WithMessage("Route is required.");
        RuleFor(x => x.ContainerBaseUrl)
            .NotEmpty().WithMessage("ContainerBaseUrl is required.")
            .Matches(@"^https?://").WithMessage("ContainerBaseUrl must start with http:// or https://.");
        RuleFor(x => x.EntrypointPath)
            .NotEmpty().WithMessage("EntrypointPath is required.");
    }
}

/// <summary>
/// Registers a new container plugin with its metadata.
/// A corresponding Keycloak client role for access control should be set up separately.
/// </summary>
public class CreatePlugin(IPluginService pluginService)
    : Endpoint<CreatePluginRequest>
{
    public override void Configure()
    {
        Post("plugins");
        Summary(s =>
        {
            s.Summary = "Registers a new container plugin to the system.";
            s.Description = "Registers a microfrontend plugin by its metadata. The plugin is identified by the combination of PluginName and Module.";
            s.Response(200, "The plugin was successfully registered.");
            s.Response(400, "The request payload is invalid.");
            s.Response(401, "The caller is not authenticated.");
            s.Response(403, "The caller does not have the required admin permissions.");
        });
    }

    public override async Task HandleAsync(CreatePluginRequest req, CancellationToken ct)
    {
        var plugin = new Plugin
        {
            Id = req.Id,
            DisplayName = req.DisplayName,
            Description = req.Description,
            ExposedModule = req.ExposedModule,
            Route = req.Route,
            ContainerUrl = req.ContainerBaseUrl,
            IconPath = req.IconPath,
            EntrypointPath = req.EntrypointPath
        };

        await pluginService.RegisterPluginAsync(plugin);

        await SendOkAsync(ct);
    }
}
