// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

namespace PluginHost.API.Services.Interfaces.Keycloak;

/// <summary>
/// Interface for interacting with Keycloak identity and access management.
/// Provides methods to manage roles, and groups.
/// </summary>
public interface IKeycloakHelper
{
    /// <summary>
    /// Authenticates with Keycloak admin API and sets the authorization header
    /// </summary>
    /// <param name="httpClient">The HttpClient to authenticate</param>
    /// <returns>Task representing the asynchronous operation</returns>
    public Task AuthenticateClient(HttpClient? httpClient = null);
}