// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

namespace PluginHost.Api.Core.Options;

/// <summary>
/// Represents the configuration options for SignalR messaging.
/// </summary>
public class SignalROptions
{
    /// <summary>
    /// The name of the configuration section for SignalR messaging options.
    /// </summary>
    public const string SectionName = "SignalRMessaging";

    /// <summary>
    /// Gets or sets the name of the SignalR event used for plugin notifications.
    /// </summary>
    public required string PluginEvent { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the specific operation names for plugin events.
    /// </summary>
    public required Operation Operation { get; set; } = new Operation();
}

/// <summary>
/// Defines the names for different plugin operations communicated via SignalR.
/// </summary>
public class Operation
{
    /// <summary>
    /// Gets or sets the operation name for adding a plugin.
    /// </summary>
    public string AddPlugin { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the operation name for deleting a plugin.
    /// </summary>
    public string DeletePlugin { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the operation name for patching plugins.
    /// </summary>
    public string PatchPlugins { get; set; } = string.Empty;
}