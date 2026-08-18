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
using PluginHost.Domain.Repositories;

namespace PluginHost.Api.Tests.Endpoints.Plugins;

public class DeletePluginEndpointTests
{
    private readonly IPluginRepository _pluginRepository;
    private readonly IPluginService _pluginService;
    private readonly DeletePlugin _endpoint;

    public DeletePluginEndpointTests()
    {
        _pluginRepository = Substitute.For<IPluginRepository>();
        _pluginService = Substitute.For<IPluginService>();
        _endpoint = Factory.Create<DeletePlugin>(_pluginRepository, _pluginService);
    }

    [Fact]
    public async Task HandleAsync_ReturnsOk_WhenPluginExists()
    {
        // Arrange
        var plugin = PluginTestData.Create("my-plugin");
        _pluginRepository.GetPluginById("my-plugin").Returns(plugin);
        _endpoint.HttpContext.Request.RouteValues["pluginId"] = "my-plugin";

        // Act
        await _endpoint.HandleAsync(new DeletePluginRequest { PluginId = "my-plugin" }, default);

        // Assert
        Assert.Equal(200, _endpoint.HttpContext.Response.StatusCode);
    }

    [Fact]
    public async Task HandleAsync_PassesExactPluginIdToService_WhenPluginExists()
    {
        // Arrange – two plugins; only the requested one must be deleted
        var plugin = PluginTestData.Create("my-plugin");
        _pluginRepository.GetPluginById("my-plugin").Returns(plugin);
        _endpoint.HttpContext.Request.RouteValues["pluginId"] = "my-plugin";

        // Act
        await _endpoint.HandleAsync(new DeletePluginRequest { PluginId = "my-plugin" }, default);

        // Assert – service called with the exact ID, not a different one
        await _pluginService.Received(1).DeletePluginAsync("my-plugin");
        await _pluginService.DidNotReceive().DeletePluginAsync(Arg.Is<string>(id => id != "my-plugin"));
    }

    [Fact]
    public async Task HandleAsync_ReturnsNotFound_WhenPluginDoesNotExist()
    {
        // Arrange
        _pluginRepository.GetPluginById("unknown").Returns((Domain.Models.Plugin?)null);
        _endpoint.HttpContext.Request.RouteValues["pluginId"] = "unknown";

        // Act
        await _endpoint.HandleAsync(new DeletePluginRequest { PluginId = "unknown" }, default);

        // Assert
        Assert.Equal(404, _endpoint.HttpContext.Response.StatusCode);
    }

    [Fact]
    public async Task HandleAsync_NeverCallsService_WhenPluginNotFound()
    {
        // Arrange – guard: service must not be invoked if the lookup returns null
        _pluginRepository.GetPluginById("unknown").Returns((Domain.Models.Plugin?)null);
        _endpoint.HttpContext.Request.RouteValues["pluginId"] = "unknown";

        // Act
        await _endpoint.HandleAsync(new DeletePluginRequest { PluginId = "unknown" }, default);

        // Assert
        await _pluginService.DidNotReceive().DeletePluginAsync(Arg.Any<string>());
    }
}
