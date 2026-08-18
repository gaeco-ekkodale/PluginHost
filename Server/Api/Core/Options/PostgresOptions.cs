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

namespace PluginHost.Api.Core.Options;

/// <summary>
/// Configuration options for PostgreSQL database connection.
/// Contains all settings required to establish a connection to the PostgreSQL database.
/// </summary>
public class PostgresOptions
{
    /// <summary>
    /// Gets the configuration section name for PostgreSQL database connection settings.
    /// </summary>
    public const string SectionName = "Postgres";

    /// <summary>
    /// Gets or sets the PostgreSQL server host address.
    /// Can be a hostname, IP address, or "localhost".
    /// </summary>
    /// <example>localhost</example>
    [RegularExpression(@"^(localhost|[a-zA-Z0-9.-]+)$", ErrorMessage = "Value for {0} must be a valid host.")]
    public string Host { get; set; } = "localhost";

    /// <summary>
    /// Gets or sets the PostgreSQL server port number.
    /// Default PostgreSQL port is 5432.
    /// </summary>
    [Range(1, 65535, ErrorMessage = "Value for {0} must be between {1} and {2}.")]
    public int Port { get; set; } = 5432;

    /// <summary>
    /// Gets or sets the name of the database to connect to.
    /// </summary>
    /// <example>app_registry</example>
    [Required(ErrorMessage = "The {0} field is required.")]
    public string Database { get; set; } = "postgres";

    /// <summary>
    /// Gets or sets the username for database authentication.
    /// </summary>
    [Required(ErrorMessage = "The {0} field is required.")]
    public string User { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the password for database authentication.
    /// </summary>
    [Required(ErrorMessage = "The {0} field is required.")]
    public string Password { get; set; } = string.Empty;
}