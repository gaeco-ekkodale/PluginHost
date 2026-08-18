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
using PluginHost.Api.Shared.DTOs;
using PluginHost.API.Services.Interfaces.Keycloak;
using System.Text;
using System.Text.Json;

namespace PluginHost.API.Services.Keycloak;

/// <summary>
/// Implementation of the IKeycloakClientManager interface.
/// Provides functionality to manage Keycloak clients, roles and tokens.
/// </summary>
public class KeycloakClientManager : IKeycloakClientManager
{
    private readonly HttpClient _http;
    private readonly KeycloakOptions _options;
    private readonly ILogger<KeycloakClientManager> _logger;
    private readonly IKeycloakHelper _keycloakHelper;
    private const string JsonMediaType = "application/json";

    /// <summary>
    /// Initializes a new instance of the KeycloakClientManager class.
    /// </summary>
    /// <param name="options">Keycloak OpenID configuration options</param>
    /// <param name="httpClientFactory">Factory for creating HTTP clients</param>
    /// <param name="logger">Logger instance</param>
    /// <param name="keycloakHelper">Helper for Keycloak operations</param>
    public KeycloakClientManager(
        IOptions<KeycloakOptions> options,
        IHttpClientFactory httpClientFactory,
        ILogger<KeycloakClientManager> logger,
        IKeycloakHelper keycloakHelper)
    {
        _http = httpClientFactory.CreateClient("KeycloakClient");
        _options = options.Value;
        _logger = logger;
        _keycloakHelper = keycloakHelper;
    }

    /// <inheritdoc />
    public async Task<string?> CreateClientWithTokenExchange(string clientName)
    {
        await _keycloakHelper.AuthenticateClient(_http);

        // Check if client exists or create a new one
        string? clientId = await GetClientIdByName(clientName);
        if (string.IsNullOrEmpty(clientId))
        {
            clientId = await CreateClient(clientName);
            if (string.IsNullOrEmpty(clientId))
            {
                return null;
            }
        }

        // Configure client for token exchange
        await CreateClientRoles(clientId, ["admin", "user"]);
        // admin is a composite role and therefore inherits everything user is allowed to do
        await AddCompositeClientRoles(clientId, "admin", ["user"]);
        await AddClientRolesToAdminGroup(clientId, ["admin"]);

        return clientId;
    }

    /// <inheritdoc />
    public async Task<TokenResponse> ExchangeToken(string accessToken, string targetClientId)
    {
        var tokenRequestData = new FormUrlEncodedContent([
            new("grant_type", "urn:ietf:params:oauth:grant-type:token-exchange"),
            new("client_id", _options.ClientId),
            new("client_secret", _options.ClientSecret),
            new("subject_token", accessToken),
            new("subject_token_type", "urn:ietf:params:oauth:token-type:access_token"),
            new("audience", targetClientId),
            new("audience", "account"),
            new("requested_token_type", "urn:ietf:params:oauth:token-type:access_token")
        ]);

        var response = await _http.PostAsync(
            $"{_options.Host}/realms/{_options.Realm}/protocol/openid-connect/token",
            tokenRequestData);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            throw new KeycloakException($"Failed to exchange token: {response.StatusCode} - {errorContent}");
        }

        var tokenResponse = await JsonSerializer.DeserializeAsync<TokenResponse>(
            await response.Content.ReadAsStreamAsync())
            ?? throw new KeycloakException("Invalid token response from Keycloak");

        return tokenResponse;
    }

    /// <inheritdoc />
    public async Task<string?> CreateClient(string clientName)
    {
        var clientData = new
        {
            clientId = clientName,
            name = $"gaeco-app: {clientName}",
            description = $"Client for {clientName} app",
            enabled = true,
            protocol = "openid-connect",
            standardFlowEnabled = false,
            directAccessGrantsEnabled = true,
            implicitFlowEnabled = true,
            publicClient = false
        };

        await _http.PostAsync(
            $"{_options.Host}/admin/realms/{_options.Realm}/clients",
            new StringContent(JsonSerializer.Serialize(clientData), Encoding.UTF8, JsonMediaType));

        return await GetClientIdByName(clientName);
    }

    /// <inheritdoc />
    public async Task<string?> GetClientIdByName(string clientName)
    {
        var response = await _http.GetAsync(
            $"{_options.Host}/admin/realms/{_options.Realm}/clients?clientId={clientName}");

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Failed to get client: {ErrorContent}",
                await response.Content.ReadAsStringAsync());
            return null;
        }

        var clients = await JsonSerializer.DeserializeAsync<JsonElement[]>(
            await response.Content.ReadAsStreamAsync());

        return clients is { Length: > 0 }
            ? clients[0].GetProperty("id").GetString()
            : null;
    }

    /// <inheritdoc />
    public async Task CreateClientRoles(string clientId, string[] roleNames)
    {
        foreach (var roleName in roleNames)
        {
            if (await CheckIfClientRoleExists(clientId, roleName))
            {
                _logger.LogInformation("Client role {RoleName} already exists for client {ClientId}",
                    roleName, clientId);
                continue;
            }

            var roleData = new { name = roleName, description = $"Role {roleName} for client" };

            await _http.PostAsync(
                $"{_options.Host}/admin/realms/{_options.Realm}/clients/{clientId}/roles",
                new StringContent(JsonSerializer.Serialize(roleData), Encoding.UTF8, JsonMediaType));
        }
    }

    /// <inheritdoc />
    public async Task<bool> CheckIfClientRoleExists(string clientId, string roleName)
    {
        var response = await _http.GetAsync(
            $"{_options.Host}/admin/realms/{_options.Realm}/clients/{clientId}/roles/{roleName}");

        return response.IsSuccessStatusCode;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteClient(string clientName)
    {
        await _keycloakHelper.AuthenticateClient(_http);

        var clientId = await GetClientIdByName(clientName);
        if (string.IsNullOrWhiteSpace(clientId))
        {
            return false;
        }

        var response = await _http.DeleteAsync(
            $"{_options.Host}/admin/realms/{_options.Realm}/clients/{clientId}");

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Failed to delete Keycloak client {ClientName}: {StatusCode}",
                clientName, response.StatusCode);
            return false;
        }

        return true;
    }

    /// <summary>
    /// Adds client roles to the Admin group
    /// </summary>
    /// <param name="clientId">The ID of the client</param>
    /// <param name="roleNames">Array of role names to add</param>
    /// <returns>True if roles were added successfully, otherwise false</returns>
    private async Task<bool> AddClientRolesToAdminGroup(string clientId, string[] roleNames)
    {
        // Find the Admin group
        var adminGroup = await GetAdminGroup();
        if (adminGroup == null)
        {
            _logger.LogWarning("Admin group not found");
            return false;
        }

        // Get and filter client roles
        var clientRoles = await GetClientRoles(clientId);
        var rolesToAdd = clientRoles?.Where(r => roleNames.Contains(r.Name)).ToList();

        if (rolesToAdd == null || !rolesToAdd.Any())
        {
            _logger.LogWarning("No matching roles found for client: {ClientId}", clientId);
            return false;
        }

        // Add client roles to the Admin group
        var addRolesUrl = $"{_options.Host}/admin/realms/{_options.Realm}/groups/{adminGroup.Id}/role-mappings/clients/{clientId}";
        var jsonContent = new StringContent(
            JsonSerializer.Serialize(rolesToAdd),
            Encoding.UTF8,
            JsonMediaType);

        var response = await _http.PostAsync(addRolesUrl, jsonContent);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Failed to add client roles to Admin group: {ErrorContent}",
                await response.Content.ReadAsStringAsync());
            return false;
        }

        return true;
    }

    /// <summary>
    /// Turns a client role into a composite role by associating other client roles with it.
    /// The parent role then inherits everything the child roles are allowed to do.
    /// </summary>
    /// <param name="clientId">The ID of the client</param>
    /// <param name="parentRoleName">The role that becomes composite (e.g. admin)</param>
    /// <param name="childRoleNames">The roles whose permissions are inherited (e.g. user)</param>
    /// <returns>True if the roles were associated successfully, otherwise false</returns>
    private async Task<bool> AddCompositeClientRoles(string clientId, string parentRoleName, string[] childRoleNames)
    {
        var clientRoles = await GetClientRoles(clientId);

        var parentRole = clientRoles?.FirstOrDefault(r => r.Name == parentRoleName);
        if (parentRole == null)
        {
            _logger.LogWarning("Parent role {RoleName} not found for client {ClientId}",
                parentRoleName, clientId);
            return false;
        }

        var childRoles = clientRoles!.Where(r => childRoleNames.Contains(r.Name)).ToList();
        if (childRoles.Count == 0)
        {
            _logger.LogWarning("No matching child roles found for client: {ClientId}", clientId);
            return false;
        }

        // Keycloak ignores roles that are already associated, so this call is idempotent
        var response = await _http.PostAsync(
            $"{_options.Host}/admin/realms/{_options.Realm}/roles-by-id/{parentRole.Id}/composites",
            new StringContent(JsonSerializer.Serialize(childRoles), Encoding.UTF8, JsonMediaType));

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Failed to add composite roles to {RoleName}: {ErrorContent}",
                parentRoleName, await response.Content.ReadAsStringAsync());
            return false;
        }

        return true;
    }

    /// <summary>
    /// Gets the Admin group from Keycloak
    /// </summary>
    /// <returns>The Admin group if found, otherwise null</returns>
    private async Task<Group?> GetAdminGroup()
    {
        var response = await _http.GetAsync($"{_options.Host}/admin/realms/{_options.Realm}/groups");

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Error retrieving groups: {ErrorContent}",
                await response.Content.ReadAsStringAsync());
            return null;
        }

        var groups = await JsonSerializer.DeserializeAsync<List<Group>>(
            await response.Content.ReadAsStreamAsync());

        return groups?.FirstOrDefault(g => g.Name == "Admin");
    }

    /// <summary>
    /// Gets the client roles from Keycloak
    /// </summary>
    /// <param name="clientId">The ID of the client</param>
    /// <returns>List of client roles if found, otherwise null</returns>
    private async Task<List<Role>?> GetClientRoles(string clientId)
    {
        var response = await _http.GetAsync(
            $"{_options.Host}/admin/realms/{_options.Realm}/clients/{clientId}/roles");

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Error retrieving client roles: {ErrorContent}",
                await response.Content.ReadAsStringAsync());
            return null;
        }

        return await JsonSerializer.DeserializeAsync<List<Role>>(
            await response.Content.ReadAsStreamAsync());
    }
}