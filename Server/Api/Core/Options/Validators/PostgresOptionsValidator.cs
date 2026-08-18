// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

using FluentValidation;

namespace PluginHost.Api.Core.Options.Validators;

/// <summary>
/// Validator for PostgreSQL database configuration options.
/// Ensures all required database connection settings are properly configured.
/// </summary>
public class PostgresOptionsValidator : AbstractValidator<PostgresOptions>
{
    public PostgresOptionsValidator()
    {
        RuleFor(x => x.Host)
            .NotEmpty()
            .WithMessage("PostgreSQL host address is required");

        RuleFor(x => x.Port)
            .InclusiveBetween(1, 65535)
            .WithMessage("PostgreSQL port must be between 1 and 65535");

        RuleFor(x => x.Database)
            .NotEmpty()
            .WithMessage("PostgreSQL database name is required");

        RuleFor(x => x.User)
            .NotEmpty()
            .WithMessage("PostgreSQL username is required");

        RuleFor(x => x.Password)
            .NotEmpty()
            .WithMessage("PostgreSQL password is required");
    }
}