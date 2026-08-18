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
using System.Text.Json.Serialization;

namespace PluginHost.Api.Shared.DTOs;

public class TokenResponse
{
    [JsonPropertyName("access_token")]
    [Required]
    public required string AccessToken { get; set; }

    [JsonPropertyName("expires_in")]
    [Required]
    public int ExpiresIn { get; set; }

    [JsonPropertyName("refresh_expires_in")]
    [Required]
    public int RefreshExpiresIn { get; set; }

    [JsonPropertyName("token_type")]
    [Required]
    public required string TokenType { get; set; }

    [JsonPropertyName("not-before-policy")]
    [Required]
    public int NotBeforePolicy { get; set; }

    [JsonPropertyName("session_state")]
    [Required]
    public required string SessionState { get; set; }

    [JsonPropertyName("scope")]
    [Required]
    public required string Scope { get; set; }
}

public class Role
{
    [JsonPropertyName("id")]
    public required string Id { get; set; }

    [JsonPropertyName("name")]
    public required string Name { get; set; }
}

public class Group
{
    [JsonPropertyName("id")]
    public required string Id { get; set; }

    [JsonPropertyName("name")]
    public required string Name { get; set; }
}