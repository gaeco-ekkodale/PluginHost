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
using FluentValidation.Results;
using Microsoft.Extensions.Options;
using PluginHost.Api.Core.Options;

namespace PluginHost.Api.Core.Extensions;

/// <summary>
/// Extension methods for setting up application configuration.
/// </summary>
public static class ConfigurationSetup
{
    /// <summary>
    /// Adds and validates application configuration sections.
    /// </summary>
    /// <param name="services">The service collection to configure</param>
    /// <param name="configuration">The configuration</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddAppConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        // Configure and validate the configuration sections
        services.AddOptions<KeycloakOptions>()
            .Bind(configuration.GetSection(KeycloakOptions.SectionName))
            .ValidateFluently()
            .ValidateOnStart();

        services.AddOptions<PostgresOptions>()
            .Bind(configuration.GetSection(PostgresOptions.SectionName))
            .ValidateFluently()
            .ValidateOnStart();


        services.AddOptions<PluginJwtOptions>()
            .Bind(configuration.GetSection(PluginJwtOptions.SectionName))
            .ValidateFluently()
            .ValidateOnStart();

        services.AddOptions<SignalROptions>()
             .Bind(configuration.GetSection(SignalROptions.SectionName))
             .ValidateFluently()
             .ValidateOnStart();

        services.AddOptions<SnapshotApiKeyOptions>()
            .Bind(configuration.GetSection(SnapshotApiKeyOptions.SectionName));

        return services;
    }

    /// <summary>
    /// Adds fluent validation for the options.
    /// </summary>
    /// <typeparam name="TOptions">The options type</typeparam>
    /// <param name="optionsBuilder">The options builder</param>
    /// <returns>The options builder for chaining</returns>
    public static OptionsBuilder<TOptions> ValidateFluently<TOptions>(this OptionsBuilder<TOptions> optionsBuilder) where TOptions : class
    {
        optionsBuilder.Services.AddSingleton<IValidateOptions<TOptions>>(x =>
            new FluentValidationOptions<TOptions>(optionsBuilder.Name, x.GetRequiredService<IValidator<TOptions>>()));
        return optionsBuilder;
    }
}

/// <summary>
/// Implementation of IValidateOptions that uses FluentValidation to validate options.
/// </summary>
/// <typeparam name="TOptions">The options type</typeparam>
public class FluentValidationOptions<TOptions> : IValidateOptions<TOptions> where TOptions : class
{
    private readonly IValidator<TOptions> _validator;

    /// <summary>
    /// The options name.
    /// </summary>
    public string? Name { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="FluentValidationOptions{TOptions}"/> class.
    /// </summary>
    /// <param name="name">The name of the options</param>
    /// <param name="validator">The validator for the options</param>
    public FluentValidationOptions(string? name, IValidator<TOptions> validator)
    {
        _validator = validator;
        Name = name;
    }

    /// <summary>
    /// Validates the options using the fluent validator.
    /// </summary>
    /// <param name="name">The name of the options being validated</param>
    /// <param name="options">The options</param>
    /// <returns>The validation result</returns>
    public ValidateOptionsResult Validate(string? name, TOptions options)
    {
        // Null name is used to configure all named options.
        if (Name != null && Name != name)
        {
            // Ignored if not validating this instance.
            return ValidateOptionsResult.Skip;
        }

        // Ensure options are provided to validate against
        ArgumentNullException.ThrowIfNull(options);

        ValidationResult? validationResult = _validator.Validate(options);
        if (validationResult.IsValid)
        {
            return ValidateOptionsResult.Success;
        }

        IEnumerable<string> errors = validationResult.Errors.Select(x =>
            $"Options validation failed for '{x.PropertyName}' with error: '{x.ErrorMessage}'.");

        return ValidateOptionsResult.Fail(errors);
    }
}