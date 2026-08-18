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

public class SignalROptionsValidator : AbstractValidator<SignalROptions>
{
    public SignalROptionsValidator()
    {
        RuleFor(x => x.PluginEvent).NotEmpty().WithMessage("Event Name für Pluginänderungen ist erforderlich.");
        RuleFor(x => x.Operation).NotNull().WithMessage("Operation muss spezifische Details zum Event enthalten.");
        RuleFor(x => x.Operation.AddPlugin).NotEmpty().WithMessage("Name der Operation zum Hinzufügen eines Plugins muss gegeben sein.");
        RuleFor(x => x.Operation.DeletePlugin).NotEmpty().WithMessage("Name der Operation zum Entfernen eines Plugins muss gegeben sein.");
        RuleFor(x => x.Operation.PatchPlugins).NotEmpty().WithMessage("Name der Operation zum Ändern eines Plugins muss gegeben sein.");
    }
}