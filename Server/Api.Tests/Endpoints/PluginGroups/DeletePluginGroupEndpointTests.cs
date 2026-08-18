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

public class DeletePluginGroupEndpointTests
{
    private readonly IPluginGroupService _pluginGroupService;
    private readonly DeletePluginGroup _endpoint;

    public DeletePluginGroupEndpointTests()
    {
        _pluginGroupService = Substitute.For<IPluginGroupService>();
        _endpoint = Factory.Create<DeletePluginGroup>(_pluginGroupService);
    }

    [Fact]
    public async Task HandleAsync_ReturnsOk_AfterDeletion()
    {
        // Arrange
        var groupId = Guid.NewGuid();
        _endpoint.HttpContext.Request.RouteValues["groupId"] = groupId.ToString();

        // Act
        await _endpoint.HandleAsync(new DeletePluginGroupRequest { GroupId = groupId }, default);

        // Assert
        Assert.Equal(200, _endpoint.HttpContext.Response.StatusCode);
    }

    [Fact]
    public async Task HandleAsync_PassesExactGroupIdToService()
    {
        // Arrange – two different IDs exist; only the requested one must be deleted
        var targetId = Guid.NewGuid();
        var otherId  = Guid.NewGuid();
        _endpoint.HttpContext.Request.RouteValues["groupId"] = targetId.ToString();

        // Act
        await _endpoint.HandleAsync(new DeletePluginGroupRequest { GroupId = targetId }, default);

        // Assert – service is called with exactly the target ID, never with the other one
        _pluginGroupService.Received(1).DeletePluginGroup(targetId);
        _pluginGroupService.DidNotReceive().DeletePluginGroup(otherId);
    }

    [Fact]
    public async Task HandleAsync_PropagatesException_WhenServiceFails()
    {
        // Arrange
        var groupId = Guid.NewGuid();
        _pluginGroupService
            .When(s => s.DeletePluginGroup(groupId))
            .Do(_ => throw new InvalidOperationException("db error"));
        _endpoint.HttpContext.Request.RouteValues["groupId"] = groupId.ToString();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _endpoint.HandleAsync(new DeletePluginGroupRequest { GroupId = groupId }, default));
    }
}
