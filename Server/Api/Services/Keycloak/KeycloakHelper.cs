// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

using Microsoft.Extensions.Options;
using PluginHost.Api.Core.Options;
using PluginHost.API.Services.Interfaces.Keycloak;
using System.Net.Http.Headers;
using System.Text.Json;

namespace PluginHost.API.Services.Keycloak;

/// <summary>
/// Implementation of the IKeycloakHelper interface.
/// Provides functionality to interact with Keycloak identity and access management.
/// </summary>
public class KeycloakHelper : IKeycloakHelper
{
    private readonly HttpClient _httpClient;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly KeycloakOptions _options;
    private readonly ILogger<KeycloakHelper> _logger;

    /// <summary>
    /// Initializes a new instance of the KeycloakHelper class.
    /// </summary>
    /// <param name="options">The Keycloak options</param>
    /// <param name="httpClientFactory">The HTTP client factory</param>
    public KeycloakHelper(IOptions<KeycloakOptions> options, IHttpClientFactory httpClientFactory, IHttpContextAccessor httpContextAccessor, ILogger<KeycloakHelper> logger)
    {
        _httpClient = httpClientFactory.CreateClient("KeycloakClient");
        _options = options.Value;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    /// <summary>
    /// Authenticates with Keycloak admin API
    /// </summary>
    /// <param name="httpClient">Optional HTTP client to authenticate. If null, authenticates the internal HTTP client.</param>
    public async Task AuthenticateClient(HttpClient? httpClient = null)
    {
        // Use the provided HTTP client or fall back to the internal one
        var client = httpClient ?? _httpClient;

        var tokenResponse = await client.PostAsync(
            $"{_options.Host}/realms/{_options.Realm}/protocol/openid-connect/token",
            new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "client_credentials"),
                new KeyValuePair<string, string>("client_id", _options.ClientId),
                new KeyValuePair<string, string>("client_secret", _options.ClientSecret)
            }));

        if (!tokenResponse.IsSuccessStatusCode)
        {
            var errorContent = await tokenResponse.Content.ReadAsStringAsync();
            _logger.LogError("Authentication failed: {ErrorContent}", errorContent);
            throw new KeycloakException($"Authentication failed: {errorContent}");
        }

        var tokenData = JsonSerializer.Deserialize<JsonElement>(
            await tokenResponse.Content.ReadAsStringAsync());

        string token = tokenData.GetProperty("access_token").GetString() ?? string.Empty;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }
}

/// <summary>
/// Exception thrown when Keycloak operations fail.
/// </summary>
public class KeycloakException : Exception
{
    /// <summary>
    /// Initializes a new instance of the KeycloakException class.
    /// </summary>
    /// <param name="message">The exception message</param>
    public KeycloakException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the KeycloakException class.
    /// </summary>
    /// <param name="message">The exception message</param>
    /// <param name="innerException">The inner exception</param>
    public KeycloakException(string message, Exception innerException) : base(message, innerException)
    {
    }
}