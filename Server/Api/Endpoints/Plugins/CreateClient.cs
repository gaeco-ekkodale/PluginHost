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
using PluginHost.API.Services.Interfaces.Keycloak;

namespace PluginHost.Api.Endpoints.Plugins;

/// <summary>
/// Request payload for creating a new Keycloak client.
/// </summary>
public class CreateClientRequest
{
    /// <summary>Name of the Keycloak client to create (passed as query parameter).</summary>
    public string ClientName { get; set; } = string.Empty;
}

public class CreateClientValidator : Validator<CreateClientRequest>
{
    public CreateClientValidator()
    {
        RuleFor(x => x.ClientName)
            .NotEmpty().WithMessage("ClientName is required.");
    }
}

/// <summary>
/// Creates a new client in Keycloak with token exchange capabilities.
/// Used to register new plugin clients in the authentication system.
/// </summary>
public class CreateClientEndpoint(IKeycloakClientManager keycloakClientManager)
    : Endpoint<CreateClientRequest, string>
{
    public override void Configure()
    {
        Post("plugins/create-client");
        Summary(s =>
        {
            s.Summary = "Creates a new client in the identity provider system.";
            s.Description = "Creates a new Keycloak client with token exchange capability. Used when registering new plugin clients that require their own authentication scope.";
            s.Response<string>(200, "Returns the ID of the created client.");
            s.Response(401, "The caller is not authenticated.");
            s.Response(403, "The caller does not have the required admin permissions.");
        });
    }

    public override async Task HandleAsync(CreateClientRequest req, CancellationToken ct)
    {
        var clientId = await keycloakClientManager.CreateClientWithTokenExchange(req.ClientName);
        await SendAsync(clientId ?? string.Empty, cancellation: ct);
    }
}
