// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PluginHost.Domain.Models;

public class Plugin
{
    // Unique identifier for the plugin, used as key and keycloak client ID
    [Key]
    public required string Id { get; set; }


    public required string DisplayName { get; set; }
    public string? Description { get; set; }

    public string? IconPath { get; set; }

    public required string EntrypointPath { get; set; }

    public required string ExposedModule { get; set; }

    public required string Route { get; set; }


    public required string ContainerUrl { get; set; }


    public Guid GroupId { get; set; }

    [ForeignKey(nameof(GroupId))]
    public PluginGroup Group { get; set; } = null!;

    public int Order { get; set; } = 0;
}
