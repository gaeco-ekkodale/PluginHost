// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

using FastEndpoints;
using NSubstitute;
using PluginHost.Api.Endpoints.Plugins;
using PluginHost.API.Services.Interfaces;
using PluginHost.Domain.Models;

namespace PluginHost.Api.Tests.Endpoints.Plugins;

public class CreatePluginEndpointTests
{
    private readonly IPluginService _pluginService;
    private readonly CreatePlugin _endpoint;

    public CreatePluginEndpointTests()
    {
        _pluginService = Substitute.For<IPluginService>();
        _endpoint = Factory.Create<CreatePlugin>(_pluginService);
    }

    private static CreatePluginRequest ValidRequest() => new()
    {
        DisplayName = "My Plugin",
        Id = "my-plugin",
        ExposedModule = "./MyModule",
        Route = "/my-plugin",
        ContainerBaseUrl = "http://my-plugin:8080",
        IconPath = "assets/icon.png",
        EntrypointPath = "assets/remoteEntry.js"
    };

    [Fact]
    public async Task HandleAsync_ReturnsOk_WhenRequestIsValid()
    {
        // Arrange
        var request = ValidRequest();

        // Act
        await _endpoint.HandleAsync(request, default);

        // Assert
        Assert.Equal(200, _endpoint.HttpContext.Response.StatusCode);
    }

    [Fact]
    public async Task HandleAsync_MapsAllRequestFieldsToPlugin()
    {
        // Arrange – capture the Plugin entity handed to the service
        Plugin? captured = null;
        _pluginService
            .When(s => s.RegisterPluginAsync(Arg.Any<Plugin>()))
            .Do(ci => captured = ci.Arg<Plugin>());

        var request = ValidRequest();

        // Act
        await _endpoint.HandleAsync(request, default);

        // Assert – every request field must survive the mapping unchanged
        Assert.NotNull(captured);
        Assert.Equal("my-plugin", captured!.Id);
        Assert.Equal("My Plugin", captured.DisplayName);
        Assert.Equal("./MyModule", captured.ExposedModule);
        Assert.Equal("/my-plugin", captured.Route);
        Assert.Equal("http://my-plugin:8080", captured.ContainerUrl);
        Assert.Equal("assets/icon.png", captured.IconPath);
        Assert.Equal("assets/remoteEntry.js", captured.EntrypointPath);
    }

    // ---------- Validator ----------

    [Fact]
    public void Validator_RejectsEmptyDisplayName()
    {
        // Arrange
        var validator = new RegisterContainerPluginValidator();
        var request = ValidRequest();
        request.DisplayName = string.Empty;

        // Act
        var result = validator.Validate(request);

        // Assert
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validator_RejectsEmptyPluginName()
    {
        // Arrange
        var validator = new RegisterContainerPluginValidator();
        var request = ValidRequest();
        request.Id = string.Empty;

        // Act
        var result = validator.Validate(request);

        // Assert
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validator_RejectsEmptyModule()
    {
        // Arrange
        var validator = new RegisterContainerPluginValidator();
        var request = ValidRequest();
        request.ExposedModule = string.Empty;

        // Act
        var result = validator.Validate(request);

        // Assert
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validator_RejectsContainerBaseUrlWithoutHttpScheme()
    {
        // Arrange
        var validator = new RegisterContainerPluginValidator();
        var request = ValidRequest();
        request.ContainerBaseUrl = "ftp://host";

        // Act
        var result = validator.Validate(request);

        // Assert
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validator_AcceptsHttpsContainerBaseUrl()
    {
        // Arrange – https:// must be valid in addition to http://
        var validator = new RegisterContainerPluginValidator();
        var request = ValidRequest();
        request.ContainerBaseUrl = "https://secure-host:8443";

        // Act
        var result = validator.Validate(request);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validator_AcceptsValidRequest()
    {
        // Arrange
        var validator = new RegisterContainerPluginValidator();

        // Act
        var result = validator.Validate(ValidRequest());

        // Assert
        Assert.True(result.IsValid);
    }
}
