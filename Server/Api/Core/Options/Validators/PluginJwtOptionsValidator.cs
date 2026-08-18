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

public class PluginJwtOptionsValidator : AbstractValidator<PluginJwtOptions>
{
    public PluginJwtOptionsValidator()
    {
        RuleFor(x => x.Secret)
             .NotEmpty().WithMessage("PluginJwt Secret ist erforderlich")
             .MinimumLength(32).WithMessage("PluginJwt Secret sollte mindestens 32 Zeichen lang sein");
        RuleFor(x => x.Issuer)
            .NotEmpty().WithMessage("PluginJwt Issuer ist erforderlich")
            .Must(uri => Uri.TryCreate(uri, UriKind.Absolute, out _))
            .WithMessage("PluginJwt Issuer muss eine gültige URL sein");
    }
}