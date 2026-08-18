// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PluginHost.Api.Core.Options;
using PluginHost.Infrastructure;

namespace PluginHost.Api.Core.Extensions;

/// <summary>
/// Extensions for data access related operations such as Entity Framework Core, Dapper, or other ORMs.
/// Configure your DbContexts, repository interfaces, and their implementations here.
/// </summary>
public static class DataAccessExtensions
{
    public static void AddPostgres(this IServiceCollection services)
    {
        services.AddDbContext<PluginHostDbContext>((provider, builder) =>
        {
            var postgresOptions = provider.GetRequiredService<IOptions<PostgresOptions>>().Value;

            builder.UseNpgsql(
                $"Host={postgresOptions.Host};" +
                $"Port={postgresOptions.Port};" +
                $"Database={postgresOptions.Database};" +
                $"Username={postgresOptions.User};" +
                $"Password={postgresOptions.Password}");
        }, ServiceLifetime.Scoped, ServiceLifetime.Scoped);
    }

    public static Task MigrateDatabase(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PluginHostDbContext>();
        return dbContext.Database.MigrateAsync();
    }
}