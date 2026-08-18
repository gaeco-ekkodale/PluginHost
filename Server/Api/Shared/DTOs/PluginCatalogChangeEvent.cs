// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

namespace PluginHost.Api.Shared.DTOs;

/// <summary>
/// Structured SignalR payload describing plugin-related changes.
/// </summary>
public class PluginChangeEvent
{
    public string ChangeType { get; set; } = "catalog";
    public string Operation { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public bool RequiresTokenRefresh { get; set; }
    public List<string> AddedPluginIds { get; set; } = [];
    public List<string> RemovedPluginIds { get; set; } = [];
    public List<PluginChangeItem> AddedPlugins { get; set; } = [];
    public List<PluginChangeItem> RemovedPlugins { get; set; } = [];
    public int? TotalPlugins { get; set; }
    public int? TotalGroups { get; set; }
    public DateTimeOffset OccurredAtUtc { get; set; } = DateTimeOffset.UtcNow;
}

public class PluginChangeItem
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Route { get; set; } = string.Empty;
}
