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
using Microsoft.AspNetCore.HttpOverrides;
using PluginHost.Api.Core.Extensions;
using PluginHost.API.Hubs;
using System.Text.Json.Serialization;

/// <summary>
/// Entry point for the API application.
/// Configures services, middleware, and runs the application.
/// </summary>
var builder = WebApplication.CreateBuilder(args);

// FluentValidation
// Register validators from assembly for configuration validation
builder.Services.AddValidatorsFromAssemblyContaining<Program>(ServiceLifetime.Singleton);

// Add HttpContextAccessor
builder.Services.AddHttpContextAccessor();

// Serialize enums as strings in API payloads
builder.Services.ConfigureHttpJsonOptions(o =>
    o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// Add Configuration
// Load and validate application configuration sections
builder.Services.AddAppConfiguration(builder.Configuration);

// Configure Logging and Compression
// Set up console logging and response compression
builder.Services.ConfigureLogging();
builder.Services.ConfigureResponseCompression();

// Configure Logging and Compression
// Set up console logging and response compression
builder.Services.ConfigureLogging();
builder.Services.ConfigureResponseCompression();

// Set up Keycloak and Authentication
// Configure JWT bearer authentication and authorization policies
builder.Services.ConfigureAuthentication();
builder.Services.ConfigureAuthorization();

// Add Services and Dependencies
// Register application services in the DI container
builder.Services.ConfigureServices();
builder.Services.AddPostgres();

// Configure Swagger and API Documentation
// Set up OpenAPI documentation with OAuth support
builder.Services.AddHealthChecks();

// Add SignalR
// Enable real-time communication with clients
builder.Services.AddSignalR();

// Configure CORS
// Set up cross-origin resource sharing policies
builder.Services.ConfigureCors(builder.Configuration);

// Add Repositories
// Register data access repositories
builder.Services.ConfigureRepositories();

// Add FastEndpoints
// Register FastEndpoints framework
builder.Services.AddFastEndpoints();

// Configure Swagger with OAuth
builder.Services.ConfigureSwagger();

// Build the application
var app = builder.Build();

// Use Global Exception Handler
app.UseGlobalExceptionHandler();

// Migrate Database
// Apply any pending migrations to the database
await app.MigrateDatabase();
// Respect reverse proxy headers (Traefik) for scheme/host
var fwdOptions = new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedHost | ForwardedHeaders.XForwardedProto
};
fwdOptions.KnownNetworks.Clear();
fwdOptions.KnownProxies.Clear();
app.UseForwardedHeaders(fwdOptions);

app.UseHttpsRedirection();
app.UseCors("AllowAllOrigins");
app.UseAuthentication();
app.UseAuthorization();
app.UseResponseCompression();

// Use FastEndpoints
app.UseFastEndpoints(c =>
{
    c.Endpoints.RoutePrefix = "api";
    c.Endpoints.ShortNames = true;
    c.Endpoints.Configurator = ep =>
    {
        // Require authorized user by default for all endpoints
        ep.AuthSchemes("Bearer");
        //ep.Policies("AdminOnly");
        ep.Description(x => x.Produces(401));
    };
});
app.UseSwaggerUIWithOAuth();

app.MapHealthChecks("health");
app.MapHub<DataHub>("/hub");

await app.RunAsync();