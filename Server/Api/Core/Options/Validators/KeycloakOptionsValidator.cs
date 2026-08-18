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

public class KeycloakOptionsValidator : AbstractValidator<KeycloakOptions>
{
    public KeycloakOptionsValidator()
    {
        RuleFor(x => x.Host).NotEmpty().WithMessage("Keycloak Host ist erforderlich");
        RuleFor(x => x.Realm).NotEmpty().WithMessage("Keycloak Authority ist erforderlich");
        RuleFor(x => x.ClientId).NotEmpty().WithMessage("Keycloak Client-ID ist erforderlich");
        RuleFor(x => x.ClientSecret).NotEmpty().WithMessage("Keycloak ClientSecret ist erforderlich");
    }
}