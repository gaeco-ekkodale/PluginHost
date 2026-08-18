// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

using PluginHost.Api.Shared.DTOs;

namespace PluginHost.API.Services.Interfaces;

/// <summary>
/// Interface for generating and validating JWT tokens for plugin access.
/// </summary>
public interface ISignedUrlService
{
    /// <summary>
    /// Generate a JWT token for the plugin
    /// </summary>
    /// <param name="pluginToken">The plugin token containing the plugin ID and optional filename</param>
    /// <returns>A signed JWT token</returns>
    public string GenerateToken(PluginToken pluginToken);

    /// <summary>
    /// Validate a JWT token
    /// </summary>
    /// <param name="token">The JWT token to validate</param>
    /// <returns>The plugin token if valid, otherwise null</returns>
    public PluginToken? ValidateJwtToken(string token);
}