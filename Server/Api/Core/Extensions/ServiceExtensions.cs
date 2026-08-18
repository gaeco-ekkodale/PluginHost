// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

using PluginHost.Api.Shared.Mappers;
using PluginHost.API.Services;
using PluginHost.API.Services.Interfaces;
using PluginHost.API.Services.Interfaces.Keycloak;
using PluginHost.API.Services.Keycloak;

namespace PluginHost.Api.Core.Extensions;

/// <summary>
/// Extension methods for configuring application services.
/// </summary>
public static class ServiceExtensions
{
    /// <summary>
    /// Configures and registers application services in the service collection.
    /// </summary>
    /// <param name="services">The service collection to configure</param>
    public static void ConfigureServices(this IServiceCollection services)
    {
        // Dependency injection for external services
        services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

        // HTTP client used to communicate with Keycloak for authentication and user management
        services.AddHttpClient("KeycloakClient");

        services.AddScoped<IKeycloakHelper, KeycloakHelper>();
        services.AddScoped<IKeycloakClientManager, KeycloakClientManager>();
        services.AddScoped<ISignedUrlService, SignedUrlService>();
        services.AddScoped<IPluginService, PluginService>();
        services.AddScoped<IPluginGroupService, PluginGroupService>();

        // FastEndpoints mappers (injected directly into endpoints)
        services.AddScoped<PluginMapper>();
    }
}