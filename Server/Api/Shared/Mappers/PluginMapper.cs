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
using PluginHost.API.Services.Interfaces;
using PluginHost.Domain.Models;

namespace PluginHost.Api.Shared.Mappers;

/// <summary>
/// Maps a <see cref="Plugin"/> entity to a <see cref="GetPluginDto"/> response,
/// including signed URLs for the remote entry and icon files.
/// </summary>
public sealed class PluginMapper(
    ISignedUrlService signedUrlService,
    IHostEnvironment hostEnvironment,
    IHttpContextAccessor httpContextAccessor)
    : ResponseMapper<GetPluginDto, Plugin>
{
    public override GetPluginDto FromEntity(Plugin e)
    {
        var host = httpContextAccessor.HttpContext!.Request.Host;
        var protocol = hostEnvironment.IsDevelopment() ? "http" : "https";

        var entrypointPath = !string.IsNullOrEmpty(e.EntrypointPath)
            ? e.EntrypointPath.TrimStart('/')
            : "remoteEntry.js";

        var iconPath = !string.IsNullOrEmpty(e.IconPath)
            ? e.IconPath.TrimStart('/')
            : "icon.png";

        var remoteEntryToken = signedUrlService.GenerateToken(new PluginToken { PluginId = e.Id });
        var fileToken = signedUrlService.GenerateToken(new PluginToken { PluginId = e.Id, Filename = iconPath });

        return new GetPluginDto
        {
            Id = e.Id,
            Name = e.DisplayName,
            Description = e.Description,
            Module = e.ExposedModule,
            Route = e.Route,
            Url = $"{protocol}://{host}/api/Plugins/{remoteEntryToken}/{entrypointPath}",
            IconUrl = !string.IsNullOrEmpty(e.IconPath) ? $"{protocol}://{host}/api/Plugins/{fileToken}/{iconPath}" : null
        };
    }

    /// <summary>
    /// Like <see cref="FromEntity"/> but omits the remote-entry URL.
    /// Used by admin list endpoints where the browser should not directly load the module.
    /// </summary>
    public GetPluginDto FromEntityWithoutRemoteEntry(Plugin e)
    {
        var dto = FromEntity(e);
        dto.Url = string.Empty;
        return dto;
    }
}
