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
using PluginHost.Api.Endpoints.PluginGroups;
using PluginHost.API.Services.Interfaces;

namespace PluginHost.Api.Tests.Endpoints.PluginGroups;

public class GetAllPluginGroupsEndpointTests
{
    private readonly IPluginGroupService _pluginGroupService;
    private readonly GetAllPluginGroups _endpoint;

    public GetAllPluginGroupsEndpointTests()
    {
        _pluginGroupService = Substitute.For<IPluginGroupService>();
        _endpoint = Factory.Create<GetAllPluginGroups>(_pluginGroupService);
    }

    [Fact]
    public async Task HandleAsync_ReturnsGroupsSortedByOrderField()
    {
        // Arrange – deliberately return groups in reverse order so sorting is observable
        var groupA = PluginGroupTestData.Create("A", order: 1);
        var groupB = PluginGroupTestData.Create("B", order: 2);
        _pluginGroupService.GetAllPluginGroups().Returns([groupB, groupA]);

        // Act
        await _endpoint.HandleAsync(default);

        // Assert – status OK and groups appear in ascending Order, not insertion order
        Assert.Equal(200, _endpoint.HttpContext.Response.StatusCode);
        var result = _endpoint.Response.ToList();
        Assert.Equal(2, result.Count);
        Assert.Equal("A", result[0].Name);
        Assert.Equal(groupA.GroupId, result[0].GroupId);
        Assert.Equal("B", result[1].Name);
        Assert.Equal(groupB.GroupId, result[1].GroupId);
    }

    [Fact]
    public async Task HandleAsync_ReturnsNotFound_WhenServiceReturnsNull()
    {
        // Arrange
        _pluginGroupService.GetAllPluginGroups().Returns((List<Domain.Models.PluginGroup>?)null);

        // Act
        await _endpoint.HandleAsync(default);

        // Assert
        Assert.Equal(404, _endpoint.HttpContext.Response.StatusCode);
    }

    [Fact]
    public async Task HandleAsync_ReturnsNotFound_WhenGroupListIsEmpty()
    {
        // Arrange
        _pluginGroupService.GetAllPluginGroups().Returns([]);

        // Act
        await _endpoint.HandleAsync(default);

        // Assert
        Assert.Equal(404, _endpoint.HttpContext.Response.StatusCode);
    }

    [Fact]
    public async Task HandleAsync_ReturnsSingleGroup_WhenOnlyOneExists()
    {
        // Arrange
        var group = PluginGroupTestData.Create("Solo", order: 0);
        _pluginGroupService.GetAllPluginGroups().Returns([group]);

        // Act
        await _endpoint.HandleAsync(default);

        // Assert – a list with exactly one element must also yield HTTP 200
        Assert.Equal(200, _endpoint.HttpContext.Response.StatusCode);
        Assert.Single(_endpoint.Response);
        Assert.Equal("Solo", _endpoint.Response.First().Name);
    }
}
