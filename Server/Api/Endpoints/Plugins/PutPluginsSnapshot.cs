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
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PluginHost.Api.Core.Options;
using PluginHost.Api.Shared.DTOs;
using PluginHost.API.Services.Interfaces;
using PluginHost.API.Services.Interfaces.Keycloak;
using PluginHost.Domain.Models;
using PluginHost.Domain.Repositories;

namespace PluginHost.Api.Endpoints.Plugins;

public enum SnapshotPluginState
{
    Running,
    Stopped
}

public class PutPluginsSnapshotItem : CreatePluginRequest
{
    public required SnapshotPluginState State { get; set; }
}

public class PutPluginsSnapshotRequest
{
    public required List<PutPluginsSnapshotItem> Plugins { get; set; }
}

public class PutPluginsSnapshotItemValidator : Validator<PutPluginsSnapshotItem>
{
    public PutPluginsSnapshotItemValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Id is required.")
            .Matches(@"^[a-z0-9\-]+$").WithMessage("Id must be lowercase alphanumeric with dashes only.");

        RuleFor(x => x.DisplayName)
            .NotEmpty().WithMessage("DisplayName is required.");

        RuleFor(x => x.ExposedModule)
            .NotEmpty().WithMessage("Module is required.");

        RuleFor(x => x.Route)
            .NotEmpty().WithMessage("Route is required.");

        RuleFor(x => x.ContainerBaseUrl)
            .NotEmpty().WithMessage("ContainerBaseUrl is required.")
            .Matches(@"^https?://").WithMessage("ContainerBaseUrl must start with http:// or https://.");

        RuleFor(x => x.EntrypointPath)
            .NotEmpty().WithMessage("EntrypointPath is required.");
    }
}

public class PutPluginsSnapshotValidator : Validator<PutPluginsSnapshotRequest>
{
    public PutPluginsSnapshotValidator()
    {
        RuleFor(x => x.Plugins)
            .NotNull().WithMessage("Plugins are required.");

        RuleForEach(x => x.Plugins)
            .SetValidator(new PutPluginsSnapshotItemValidator());

        RuleFor(x => x.Plugins)
            .Must(list => list.Select(p => p.Id).Distinct(StringComparer.Ordinal).Count() == list.Count)
            .WithMessage("Id values in snapshot must be unique.");

        RuleFor(x => x.Plugins)
            .Must(list => list.Select(p => p.Route).Distinct(StringComparer.Ordinal).Count() == list.Count)
            .WithMessage("Route values in snapshot must be unique.");
    }
}

/// <summary>
/// Applies a full snapshot of plugins sent by an external service.
/// Reconciles inserts, updates, enable/disable transitions, and removals.
/// </summary>
public class PutPluginsSnapshot(
    IPluginRepository pluginRepository,
    IPluginService pluginService,
    IKeycloakClientManager keycloakClientManager,
    IOptions<SignalROptions> signalROptions)
    : Endpoint<PutPluginsSnapshotRequest>
{
    public override void Configure()
    {
        Put("plugins");
        AuthSchemes("ApiKey");
        Summary(s =>
        {
            s.Summary = "Applies a complete plugin snapshot.";
            s.Description = "Accepts a complete plugin list from an external orchestrator and reconciles DB + Keycloak clients (insert/update/enable/disable/delete). Requires API key in header X-API-Key (or configured header name).";
            s.Response(200, "Snapshot was applied successfully.");
            s.Response(400, "Snapshot payload is invalid.");
            s.Response(401, "Missing or invalid API key.");
        });
    }

    public override async Task HandleAsync(PutPluginsSnapshotRequest req, CancellationToken ct)
    {
        try
        {
            // Group and order are owned by the host, not by the snapshot sender: the repository
            // keeps them for known plugins and assigns them for new ones.
            var snapshotEntities = req.Plugins.Select(snapshotPlugin => new Plugin
            {
                Id = snapshotPlugin.Id,
                DisplayName = snapshotPlugin.DisplayName,
                Description = snapshotPlugin.Description,
                ExposedModule = snapshotPlugin.ExposedModule,
                Route = snapshotPlugin.Route.StartsWith("/") ? snapshotPlugin.Route : "/" + snapshotPlugin.Route,
                ContainerUrl = snapshotPlugin.ContainerBaseUrl,
                IconPath = snapshotPlugin.IconPath,
                EntrypointPath = snapshotPlugin.EntrypointPath,
                GroupId = Guid.Empty,
                Order = 0
            }).ToList();

            // Which plugins were added/removed is decided inside the snapshot transaction.
            // Computing it here from a separate read would be wrong under concurrent snapshots:
            // a plugin inserted by a parallel request would show up as "added" in both requests
            // (or in neither), leaving Keycloak clients uncreated and clients unnotified.
            var snapshotResult = pluginRepository.ApplyPluginSnapshot(snapshotEntities);
            var addedPluginIds = snapshotResult.AddedPluginIds.ToList();
            var removedPluginIds = snapshotResult.RemovedPlugins.Select(p => p.Id).ToList();

            // Create Keycloak clients for newly added plugins. The DB snapshot is already
            // committed and is the source of truth, so a Keycloak failure for a single plugin
            // must NOT abort the whole request: otherwise the remaining clients are skipped and
            // – worse – the change notification below never fires, leaving freshly added plugins
            // invisible to the frontends ("not all plugins applied"). Isolate each call.
            var keycloakFailures = new List<string>();
            foreach (var pluginId in addedPluginIds)
            {
                try
                {
                    await keycloakClientManager.CreateClientWithTokenExchange(pluginId);
                }
                catch (Exception ex)
                {
                    keycloakFailures.Add(pluginId);
                    Logger.LogError(ex,
                        "Failed to create Keycloak client for plugin {PluginId}; snapshot continues without aborting.",
                        pluginId);
                }
            }

            // Keycloak clients are intentionally NOT deleted when a plugin disappears from the
            // snapshot. Clients are kept so they can be reused when the plugin is re-registered
            // and to avoid destructive Keycloak changes on transient snapshot gaps.
            // foreach (var pluginId in removedPluginIds)
            // {
            //     await keycloakClientManager.DeleteClient(pluginId);
            // }

            if (keycloakFailures.Count > 0)
            {
                Logger.LogWarning(
                    "Plugin snapshot applied, but Keycloak client creation failed for {FailureCount} plugin(s): {PluginIds}.",
                    keycloakFailures.Count, string.Join(", ", keycloakFailures));
            }

            if (addedPluginIds.Count > 0 || removedPluginIds.Count > 0)
            {
                var removedPlugins = snapshotResult.RemovedPlugins
                    .Select(p => new PluginChangeItem
                    {
                        Id = p.Id,
                        Name = p.DisplayName,
                        Route = p.Route
                    })
                    .ToList();

                var addedPlugins = req.Plugins
                    .Where(p => addedPluginIds.Contains(p.Id, StringComparer.Ordinal))
                    .Select(p => new PluginChangeItem
                    {
                        Id = p.Id,
                        Name = p.DisplayName,
                        Route = p.Route.StartsWith("/") ? p.Route : "/" + p.Route
                    })
                    .ToList();

                var message = $"Plugin-Katalog aktualisiert: {addedPluginIds.Count} hinzugefügt, {removedPluginIds.Count} entfernt.";
                await pluginService.PublishPluginChangeAsync(new PluginChangeEvent
                {
                    ChangeType = "catalog",
                    Operation = signalROptions.Value.Operation.PatchPlugins,
                    Message = message,
                    Source = "snapshot",
                    RequiresTokenRefresh = true,
                    AddedPluginIds = addedPluginIds,
                    RemovedPluginIds = removedPluginIds,
                    AddedPlugins = addedPlugins,
                    RemovedPlugins = removedPlugins,
                    TotalPlugins = snapshotEntities.Count
                });
            }

            await SendOkAsync(ct);
        }
        catch (InvalidOperationException ex)
        {
            AddError(ex.Message);
            await SendErrorsAsync(400, ct);
        }
        catch (DbUpdateException ex)
        {
            AddError("Snapshot violates database constraints.");
            AddError(ex.InnerException?.Message ?? ex.Message);
            await SendErrorsAsync(400, ct);
        }
    }
}
