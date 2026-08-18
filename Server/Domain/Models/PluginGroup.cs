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

namespace PluginHost.Domain.Models;

public class PluginGroup
{
    [Key]
    public required Guid GroupId { get; set; }

    public required string Name { get; set; }

    public int Order { get; set; } = 0;

    public virtual ICollection<Plugin> Plugins { get; set; } = [];
}
