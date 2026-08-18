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

namespace PluginHost.Api.Endpoints.Plugins;

/// <summary>
/// Request payload for token exchange.
/// Note: compared to the old controller, the access token is now sent as a JSON object
/// property instead of a raw JSON string body: { "accessToken": "..." }
/// </summary>
public class ExchangeTokenRequest
{
    /// <summary>The Keycloak client ID to exchange the token for (from route).</summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>The current bearer access token to exchange.</summary>
    public string AccessToken { get; set; } = string.Empty;
}

public class ExchangeTokenValidator : Validator<ExchangeTokenRequest>
{
    public ExchangeTokenValidator()
    {
        RuleFor(x => x.ClientId)
            .NotEmpty().WithMessage("ClientId is required.");
        RuleFor(x => x.AccessToken)
            .NotEmpty().WithMessage("AccessToken is required.");
    }
}

/// <summary>
/// Exchanges an access token for a new token scoped to a specific plugin client.
/// Used in the microfrontend architecture for secure cross-plugin communication.
/// </summary>
public class ExchangeTokenEndpoint(IPluginService pluginService)
    : Endpoint<ExchangeTokenRequest, TokenResponse>
{
    public override void Configure()
    {
        Post("plugins/{clientId}/token");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Exchanges an access token for a new token valid for a specific client.";
            s.Description = "Allows authenticated users to exchange their current access token for one that can be used with a specific plugin client.";
            s.Response<TokenResponse>(200, "Returns the new token response.");
            s.Response(400, "The access token or client ID is missing or invalid.");
        });
    }

    public override async Task HandleAsync(ExchangeTokenRequest req, CancellationToken ct)
    {
        var result = await pluginService.ExchangeToken(req.AccessToken, req.ClientId);
        await SendAsync(result, cancellation: ct);
    }
}
