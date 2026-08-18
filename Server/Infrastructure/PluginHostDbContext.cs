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
using PluginHost.Domain.Models;

namespace PluginHost.Infrastructure;

/// <summary>
/// Entity Framework Core database context for the Plugin Host application.
/// Manages the database schema and entity relationships for plugins and plugin groups.
/// </summary>
public class PluginHostDbContext : DbContext
{
    /// <summary>
    /// Gets or sets the DbSet for Plugin entities.
    /// </summary>
    public DbSet<Plugin> Plugins { get; set; }

    /// <summary>
    /// Gets or sets the DbSet for PluginGroup entities.
    /// </summary>
    public DbSet<PluginGroup> PluginGroups { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="PluginHostDbContext"/> class.
    /// </summary>
    /// <param name="options">The options to be used by the DbContext.</param>
    public PluginHostDbContext(DbContextOptions<PluginHostDbContext> options) : base(options)
    {
    }

    /// <summary>
    /// Configures the entity relationships and database schema.
    /// </summary>
    /// <param name="modelBuilder">The builder being used to construct the model for the context.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Plugin>(p =>
        {
            p.HasKey(e => e.Id);
            p.HasIndex(e => e.Route).IsUnique();
        });

        modelBuilder.Entity<PluginGroup>(p =>
        {
            p.HasKey(e => e.GroupId);
            p.HasMany(p => p.Plugins)
                .WithOne(p => p.Group)
                .HasForeignKey(p => p.GroupId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
