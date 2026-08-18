// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

namespace PluginHost.Domain.Models;

/// <summary>
/// Reports which plugins were actually inserted and removed while a snapshot was applied.
/// The values are determined inside the snapshot transaction, so callers must not recompute
/// them from a separate read: concurrent snapshots would otherwise report stale additions.
/// </summary>
/// <param name="AddedPluginIds">Ids of plugins that this snapshot inserted.</param>
/// <param name="RemovedPlugins">Plugins that this snapshot deleted, as they looked before deletion.</param>
public record PluginSnapshotResult(
    IReadOnlyList<string> AddedPluginIds,
    IReadOnlyList<Plugin> RemovedPlugins);
