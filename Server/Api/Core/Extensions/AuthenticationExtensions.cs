// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using PluginHost.Api.Core.Options;

namespace PluginHost.Api.Core.Extensions;

/// <summary>
/// Extensions for configuring authentication and authorization.
/// </summary>
public static class AuthenticationExtensions
{
    /// <summary>
    /// Configures JWT authentication using Keycloak as the identity provider.
    /// </summary>
    /// <param name="services">The service collection to configure</param>
    public static void ConfigureAuthentication(this IServiceCollection services)
    {
        var serviceProvider = services.BuildServiceProvider();
        var opts = serviceProvider.GetRequiredService<IOptions<KeycloakOptions>>().Value;
        var env = serviceProvider.GetRequiredService<IWebHostEnvironment>();

        var authorityUrl = $"{opts.Host}/realms/{opts.Realm}";
        var keycloakUsesHttps = opts.Host.StartsWith("https://", StringComparison.OrdinalIgnoreCase);

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Authority = authorityUrl;
                Console.WriteLine($"Configuring JWT Authentication with Authority: {options.Authority}");
                options.IncludeErrorDetails = true;
                options.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = context =>
                    {
                        // Log the exception or handle it as needed
                        Console.WriteLine($"Authentication failed: {context.Exception.Message}");
                        return Task.CompletedTask;
                    }
                };

                // Configure multiple audiences
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateAudience = !env.IsDevelopment(),
                    ValidateIssuer = !env.IsDevelopment(),
                    ValidateLifetime = !env.IsDevelopment(),
                    ValidAudience = "account",
                    NameClaimType = "preferred_username",
                    RoleClaimType = "groups"
                };

                // Allow HTTP metadata when Keycloak is on HTTP (e.g. Docker internal)
                options.RequireHttpsMetadata = keycloakUsesHttps;
            })
            .AddScheme<AuthenticationSchemeOptions, ApiKeyAuthenticationHandler>(
                ApiKeyAuthenticationDefaults.AuthenticationScheme, _ => { });
    }

    /// <summary>
    /// Configures authorization policies for the application.
    /// </summary>
    /// <param name="services">The service collection to configure</param>
    public static void ConfigureAuthorization(this IServiceCollection services)
    {
        services.AddAuthorizationBuilder()
            .AddPolicy("AdminOnly", policy => policy.RequireClaim("groups", "/Admin"))
            .AddPolicy("UserOnly", policy => policy.RequireClaim("groups", "/User"));

    }
}