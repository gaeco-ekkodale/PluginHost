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

public class KeycloakOptions
{
    public const string SectionName = "Keycloak";
    public required string Host { get; set; }
    public required string Realm { get; set; }
    public required string ClientId { get; set; }
    public required string ClientSecret { get; set; }
}