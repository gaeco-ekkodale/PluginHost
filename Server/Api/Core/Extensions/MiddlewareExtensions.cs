// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

using Microsoft.AspNetCore.ResponseCompression;
using PluginHost.Api.Core.Middleware;
using System.IO.Compression;


namespace PluginHost.Api.Core.Extensions;

/// <summary>
/// Extension methods for configuring various middleware components.
/// </summary>
public static class MiddlewareExtensions
{
    /// <summary>
    /// Configures logging for the application.
    /// </summary>
    /// <param name="services">The service collection to configure</param>
    public static void ConfigureLogging(this IServiceCollection services)
    {
        services.AddLogging(logging =>
        {
            logging.ClearProviders();
            logging.AddConsole();
        });
    }

    /// <summary>
    /// Configures response compression middleware.
    /// </summary>
    /// <param name="services">The service collection to configure</param>
    public static void ConfigureResponseCompression(this IServiceCollection services)
    {
        services.AddResponseCompression(options =>
        {
            options.EnableForHttps = true;
            options.Providers.Add<GzipCompressionProvider>();
        });

        services.Configure<GzipCompressionProviderOptions>(options =>
        {
            options.Level = CompressionLevel.Fastest;
        });
    }

    /// <summary>
    /// Configures CORS policies for the application.
    /// </summary>
    /// <param name="services">The service collection to configure</param>
    /// <param name="configuration">Application configuration</param>
    public static void ConfigureCors(this IServiceCollection services, IConfiguration configuration)
    {
        var allowedOrigins = configuration.GetSection("AllowedOrigins").Get<string[]>() ?? [];

        services.AddCors(options =>
        {
            options.AddPolicy("AllowAllOrigins", policy =>
            {
                //if in dev allow all origins, otherwise only allow configured origins
                if (allowedOrigins.Length == 0)
                    policy.AllowAnyOrigin()
                          .AllowAnyMethod()
                          .AllowAnyHeader();

                else
                    policy.WithOrigins(allowedOrigins)
                      .AllowAnyMethod()
                      .AllowAnyHeader();
            });
        });
    }

    /// <summary>
    /// Configures the global exception handler middleware.
    /// </summary>
    /// <param name="app">The application builder</param>
    public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder app)
    {
        return app.UseMiddleware<ExceptionHandlingMiddleware>();
    }
}