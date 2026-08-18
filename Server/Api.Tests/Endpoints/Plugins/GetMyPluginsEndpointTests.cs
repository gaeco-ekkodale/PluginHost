// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

using System.Security.Claims;
using FastEndpoints;
using Microsoft.AspNetCore.Http;
using NSubstitute;
using PluginHost.Api.Endpoints.Plugins;
using PluginHost.Domain.Repositories;

namespace PluginHost.Api.Tests.Endpoints.Plugins;

public class GetMyPluginsEndpointTests
{
    private readonly IPluginRepository _pluginRepository;

    public GetMyPluginsEndpointTests()
    {
        _pluginRepository = Substitute.For<IPluginRepository>();
    }

    private GetMyPlugins CreateEndpointWithLicenses(params string[] licenseIds)
    {
        var mapper = PluginEndpointTestHelper.CreateMapper();
        var endpoint = Factory.Create<GetMyPlugins>(_pluginRepository, mapper);

        var claims = licenseIds.Select(id => new Claim("aud", id)).ToArray();
        var identity = new ClaimsIdentity(claims, authenticationType: "Test");
        endpoint.HttpContext.User = new ClaimsPrincipal(identity);

        return endpoint;
    }

    [Fact]
    public async Task HandleAsync_ReturnsOnlyLicensedPlugins()
    {
        // Arrange – user holds licenses for plugin-a and plugin-c; plugin-b must be excluded
        _pluginRepository.GetAllPlugins().Returns(
        [
            PluginTestData.Create("plugin-a"),
            PluginTestData.Create("plugin-b"),
            PluginTestData.Create("plugin-c")
        ]);

        // Act
        var endpoint = CreateEndpointWithLicenses("plugin-a", "plugin-c");
        await endpoint.HandleAsync(default);

        // Assert
        Assert.Equal(200, endpoint.HttpContext.Response.StatusCode);
        var result = endpoint.Response.ToList();
        Assert.Equal(2, result.Count);
        Assert.Contains(result, d => d.Id == "plugin-a");
        Assert.DoesNotContain(result, d => d.Id == "plugin-b");
        Assert.Contains(result, d => d.Id == "plugin-c");
    }

    [Fact]
    public async Task HandleAsync_ReturnsEmptyList_WhenUserHasNoLicenses()
    {
        // Arrange
        _pluginRepository.GetAllPlugins().Returns([PluginTestData.Create("plugin-a")]);

        // Act
        var endpoint = CreateEndpointWithLicenses();
        await endpoint.HandleAsync(default);

        // Assert
        Assert.Equal(200, endpoint.HttpContext.Response.StatusCode);
        Assert.Empty(endpoint.Response);
    }

    [Fact]
    public async Task HandleAsync_ReturnsEmptyList_WhenNoPluginMatchesLicense()
    {
        // Arrange – user has a license but no registered plugin matches it
        _pluginRepository.GetAllPlugins().Returns([PluginTestData.Create("plugin-x")]);

        // Act
        var endpoint = CreateEndpointWithLicenses("plugin-a");
        await endpoint.HandleAsync(default);

        // Assert
        Assert.Equal(200, endpoint.HttpContext.Response.StatusCode);
        Assert.Empty(endpoint.Response);
    }

    [Fact]
    public async Task HandleAsync_ReturnsEmptyList_WhenUserIsNotAuthenticated()
    {
        // Arrange – unauthenticated identity has no IsAuthenticated flag
        _pluginRepository.GetAllPlugins().Returns([PluginTestData.Create("plugin-a")]);
        var mapper = PluginEndpointTestHelper.CreateMapper();
        var endpoint = Factory.Create<GetMyPlugins>(_pluginRepository, mapper);
        endpoint.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity());

        // Act
        await endpoint.HandleAsync(default);

        // Assert
        Assert.Equal(200, endpoint.HttpContext.Response.StatusCode);
        Assert.Empty(endpoint.Response);
    }

    [Fact]
    public async Task HandleAsync_IncludesSignedUrlsInResult()
    {
        // Arrange
        _pluginRepository.GetAllPlugins().Returns([PluginTestData.Create("plugin-a")]);

        // Act
        var endpoint = CreateEndpointWithLicenses("plugin-a");
        await endpoint.HandleAsync(default);

        // Assert – the full-access response includes a remote-entry URL (unlike admin listing)
        var dto = endpoint.Response.First();
        Assert.NotEmpty(dto.Url);
        Assert.NotEmpty(dto.IconUrl);
    }

    [Fact]
    public async Task HandleAsync_ReturnsPluginOnce_WhenUserHasDuplicateLicenseClaim()
    {
        // Arrange – the same aud claim appears twice (misconfigured token)
        _pluginRepository.GetAllPlugins().Returns([PluginTestData.Create("plugin-a")]);

        // Act – two "aud" claims for the same plugin
        var endpoint = CreateEndpointWithLicenses("plugin-a", "plugin-a");
        await endpoint.HandleAsync(default);

        // Assert – the plugin must appear exactly once; the endpoint filters by PluginId
        var result = endpoint.Response.ToList();
        Assert.Single(result);
        Assert.Equal("plugin-a", result[0].Id);
    }
}
