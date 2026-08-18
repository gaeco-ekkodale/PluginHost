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
using NSubstitute;
using PluginHost.Api.Endpoints.PluginMenu;
using PluginHost.Api.Tests.Endpoints.Plugins;
using PluginHost.API.Services.Interfaces;
using PluginHost.Domain.Models;
using PluginHost.Domain.Repositories;

namespace PluginHost.Api.Tests.Endpoints.PluginMenu;

public class GetPluginMenuEndpointTests
{
    private readonly IPluginGroupService _pluginGroupService;
    private readonly IPluginRepository _pluginRepository;

    public GetPluginMenuEndpointTests()
    {
        _pluginGroupService = Substitute.For<IPluginGroupService>();
        _pluginRepository = Substitute.For<IPluginRepository>();
    }

    private GetPluginMenu CreateEndpointWithLicenses(params string[] licenseIds)
    {
        var mapper = PluginEndpointTestHelper.CreateMapper();
        var endpoint = Factory.Create<GetPluginMenu>(_pluginGroupService, _pluginRepository, mapper);

        var claims = licenseIds.Select(id => new Claim("aud", id)).ToArray();
        var identity = new ClaimsIdentity(claims, authenticationType: "Test");
        endpoint.HttpContext.User = new ClaimsPrincipal(identity);

        return endpoint;
    }

    [Fact]
    public async Task HandleAsync_ReturnsGroupsSortedByOrderField()
    {
        // Arrange – group A has a higher Order than B; the response must flip them
        var groupA = new PluginGroup { GroupId = Guid.NewGuid(), Name = "A", Order = 2 };
        var groupB = new PluginGroup { GroupId = Guid.NewGuid(), Name = "B", Order = 1 };
        _pluginGroupService.GetAllPluginGroups().Returns([groupA, groupB]);
        _pluginRepository.GetAllPlugins().Returns([]);

        // Act
        var endpoint = CreateEndpointWithLicenses();
        await endpoint.HandleAsync(default);

        // Assert – B (Order=1) must come before A (Order=2)
        Assert.Equal(200, endpoint.HttpContext.Response.StatusCode);
        var result = endpoint.Response.ToList();
        Assert.Equal(2, result.Count);
        Assert.Equal("B", result[0].Name);
        Assert.Equal("A", result[1].Name);
    }

    [Fact]
    public async Task HandleAsync_PlacesPluginsIntoCorrectGroup()
    {
        // Arrange – plugin belongs to a specific group
        var groupId = Guid.NewGuid();
        var group = new PluginGroup { GroupId = groupId, Name = "Group1", Order = 0 };
        var plugin = PluginTestData.Create("plugin-a");
        plugin.GroupId = groupId;

        _pluginGroupService.GetAllPluginGroups().Returns([group]);
        _pluginRepository.GetAllPlugins().Returns([plugin]);

        // Act
        var endpoint = CreateEndpointWithLicenses("plugin-a");
        await endpoint.HandleAsync(default);

        // Assert – the plugin appears in the correct group
        var menu = endpoint.Response.ToList();
        Assert.Single(menu);
        Assert.Equal("Group1", menu[0].Name);
        Assert.Single(menu[0].Plugins);
        Assert.Equal("plugin-a", menu[0].Plugins[0].Id);
    }

    [Fact]
    public async Task HandleAsync_OrdersPluginsWithinGroup_ByOrderField()
    {
        // Arrange – two plugins in the same group with explicit Order values
        var groupId = Guid.NewGuid();
        var group = new PluginGroup { GroupId = groupId, Name = "G", Order = 0 };

        var first  = PluginTestData.Create("plugin-first");  first.GroupId  = groupId; first.Order  = 1;
        var second = PluginTestData.Create("plugin-second"); second.GroupId = groupId; second.Order = 0;

        _pluginGroupService.GetAllPluginGroups().Returns([group]);
        _pluginRepository.GetAllPlugins().Returns([first, second]);

        // Act – user is licensed for both
        var endpoint = CreateEndpointWithLicenses("plugin-first", "plugin-second");
        await endpoint.HandleAsync(default);

        // Assert – plugin with Order=0 (second) must come before Order=1 (first)
        var plugins = endpoint.Response.First().Plugins;
        Assert.Equal("plugin-second", plugins[0].Id);
        Assert.Equal("plugin-first",  plugins[1].Id);
    }

    [Fact]
    public async Task HandleAsync_ExcludesPluginsUserHasNoLicenseFor()
    {
        // Arrange – both plugins belong to the same group; user only holds one license
        var groupId = Guid.NewGuid();
        var group = new PluginGroup { GroupId = groupId, Name = "G", Order = 0 };
        var pluginA = PluginTestData.Create("plugin-a"); pluginA.GroupId = groupId;
        var pluginB = PluginTestData.Create("plugin-b"); pluginB.GroupId = groupId;

        _pluginGroupService.GetAllPluginGroups().Returns([group]);
        _pluginRepository.GetAllPlugins().Returns([pluginA, pluginB]);

        // Act
        var endpoint = CreateEndpointWithLicenses("plugin-a");
        await endpoint.HandleAsync(default);

        // Assert – only the licensed plugin is in the menu
        var groupDto = endpoint.Response.First();
        Assert.Single(groupDto.Plugins);
        Assert.Equal("plugin-a", groupDto.Plugins[0].Id);
    }

    [Fact]
    public async Task HandleAsync_ExcludesPlugin_WhenAssignedGroupDoesNotExist()
    {
        // Arrange – plugin references a GroupId that has no corresponding group
        var knownGroupId   = Guid.NewGuid();
        var unknownGroupId = Guid.NewGuid();
        var group  = new PluginGroup { GroupId = knownGroupId, Name = "Known", Order = 0 };
        var orphan = PluginTestData.Create("orphan"); orphan.GroupId = unknownGroupId;

        _pluginGroupService.GetAllPluginGroups().Returns([group]);
        _pluginRepository.GetAllPlugins().Returns([orphan]);

        // Act
        var endpoint = CreateEndpointWithLicenses("orphan");
        await endpoint.HandleAsync(default);

        // Assert – the known group has an empty Plugins list; the orphaned plugin does not appear
        var groupDto = endpoint.Response.First();
        Assert.Empty(groupDto.Plugins);
    }

    [Fact]
    public async Task HandleAsync_ReturnsEmptyGroupPlugins_WhenUserHasNoLicenses()
    {
        // Arrange
        var groupId = Guid.NewGuid();
        var group = new PluginGroup { GroupId = groupId, Name = "G", Order = 0 };
        var plugin = PluginTestData.Create("plugin-a"); plugin.GroupId = groupId;

        _pluginGroupService.GetAllPluginGroups().Returns([group]);
        _pluginRepository.GetAllPlugins().Returns([plugin]);

        // Act
        var endpoint = CreateEndpointWithLicenses();
        await endpoint.HandleAsync(default);

        // Assert – group is present but empty
        var groupDto = endpoint.Response.First();
        Assert.Empty(groupDto.Plugins);
    }

    [Fact]
    public async Task HandleAsync_ReturnsEmptyMenu_WhenNoGroupsExist()
    {
        // Arrange
        _pluginGroupService.GetAllPluginGroups().Returns((List<PluginGroup>?)null);
        _pluginRepository.GetAllPlugins().Returns([]);

        // Act
        var endpoint = CreateEndpointWithLicenses("plugin-a");
        await endpoint.HandleAsync(default);

        // Assert
        Assert.Equal(200, endpoint.HttpContext.Response.StatusCode);
        Assert.Empty(endpoint.Response);
    }
}
