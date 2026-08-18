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

namespace PluginHost.API.Services.Interfaces.Keycloak;

/// <summary>
/// Interface for managing Keycloak client operations including creation, configuration, and token exchange
/// </summary>
public interface IKeycloakClientManager
{
    /// <summary>
    /// Creates a new Keycloak client with token exchange capability and optional roles
    /// </summary>
    /// <param name="clientName">Name of the client to create</param>
    /// <returns>The ID of the created client, or null if creation failed</returns>
    Task<string?> CreateClientWithTokenExchange(string clientName);

    /// <summary>
    /// Exchanges a token from the source client to a target client
    /// </summary>
    /// <param name="accessToken">Current access token</param>
    /// <param name="targetClientId">Target client ID to exchange the token for</param>
    /// <returns>Token response with new access token</returns>
    Task<TokenResponse> ExchangeToken(string accessToken, string targetClientId);

    /// <summary>
    /// Creates a basic OpenID Connect client
    /// </summary>
    /// <param name="clientName">The name of the client to create</param>
    /// <returns>The ID of the created client, or null if creation failed</returns>
    Task<string?> CreateClient(string clientName);

    /// <summary>
    /// Gets a client ID by name
    /// </summary>
    /// <param name="clientName">The name of the client</param>
    /// <returns>The client ID if found, otherwise null</returns>
    Task<string?> GetClientIdByName(string clientName);

    /// <summary>
    /// Creates roles for a client
    /// </summary>
    /// <param name="clientId">The ID of the client</param>
    /// <param name="roleNames">Array of role names to create</param>
    /// <returns>A task representing the asynchronous operation</returns>
    Task CreateClientRoles(string clientId, string[] roleNames);

    /// <summary>
    /// Checks if a client role exists
    /// </summary>
    /// <param name="clientId">The ID of the client</param>
    /// <param name="roleName">The name of the role to check</param>
    /// <returns>True if the role exists, otherwise false</returns>
    Task<bool> CheckIfClientRoleExists(string clientId, string roleName);

    /// <summary>
    /// Deletes a Keycloak client by client name.
    /// </summary>
    /// <param name="clientName">The configured client name (plugin ID)</param>
    /// <returns>True when operation succeeded; false when client was not found or delete failed</returns>
    Task<bool> DeleteClient(string clientName);
}