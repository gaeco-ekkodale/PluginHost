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
using Microsoft.IdentityModel.Tokens;
using PluginHost.API.Services.Interfaces;

namespace PluginHost.Api.Endpoints.Plugins;

/// <summary>
/// Request payload for retrieving a plugin file via signed token.
/// </summary>
public class GetPluginFileRequest
{
    /// <summary>Signed JWT that grants access to the plugin's files.</summary>
    [BindFrom("token")]
    public string Token { get; set; } = string.Empty;

    }

/// <summary>
/// Serves plugin files (JS modules, icons, etc.) by proxying them from the plugin container.
/// Uses a signed token for validation, allowing secure public access without login.
/// This endpoint intentionally allows anonymous access so microfrontends can load freely.
/// Supports sub-paths via the catch-all route segment.
/// </summary>
public class GetPluginFile(IPluginService pluginService)
    : Endpoint<GetPluginFileRequest>
{
    public override void Configure()
    {
        Get("plugins/{token}/{**filename}");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Retrieves a plugin file by token and file path.";
            s.Description = "Proxies a file from the plugin container after validating the signed URL token. No user login is required; the signed token provides scoped access.";
            s.Response(200, "Returns the requested file with its correct content type.");
            s.Response(401, "The signed token has expired.");
            s.Response(403, "The token does not grant access to the requested file.");
            s.Response(404, "The requested file was not found in the plugin container.");
        });
    }

    public override async Task HandleAsync(GetPluginFileRequest req, CancellationToken ct)
    {
        var filename = HttpContext.Request.RouteValues["filename"]?.ToString() ?? string.Empty;
        try
        {
            var (stream, contentType) = await pluginService.GetPluginFileAsync(req.Token, filename);
            await SendStreamAsync(stream, contentType: contentType, cancellation: ct);
        }
        catch (SecurityTokenExpiredException)
        {
            ThrowError("JWT token has expired.", 401);
        }
        catch (UnauthorizedAccessException)
        {
            ThrowError("Access denied.", 403);
        }
        catch (FileNotFoundException)
        {
            await SendNotFoundAsync(ct);
        }
    }
}
