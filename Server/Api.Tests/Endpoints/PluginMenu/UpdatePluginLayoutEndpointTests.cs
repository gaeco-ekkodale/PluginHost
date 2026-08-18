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
using PluginHost.Api.Endpoints.PluginMenu;
using PluginHost.API.Services.Interfaces;
using PluginHost.Domain.Models;

namespace PluginHost.Api.Tests.Endpoints.PluginMenu;

public class UpdatePluginLayoutEndpointTests
{
    private readonly IPluginGroupService _pluginGroupService;
    private readonly IPluginService _pluginService;
    private readonly UpdatePluginLayout _endpoint;

    public UpdatePluginLayoutEndpointTests()
    {
        _pluginGroupService = Substitute.For<IPluginGroupService>();
        _pluginService = Substitute.For<IPluginService>();
        _endpoint = Factory.Create<UpdatePluginLayout>(_pluginGroupService, _pluginService);
    }

    [Fact]
    public async Task HandleAsync_ReturnsOk_WhenPayloadIsValid()
    {
        // Arrange
        var groupId = Guid.NewGuid();
        _pluginGroupService.GetAllPluginGroups().Returns([new PluginGroup { GroupId = groupId, Name = "G" }]);
        var request = new UpdatePluginLayoutRequest
        {
            Groups = [new PluginLayoutGroupEntry { GroupId = groupId, Name = "G", Plugins = ["p1"] }]
        };

        // Act
        await _endpoint.HandleAsync(request, default);

        // Assert
        Assert.Equal(200, _endpoint.HttpContext.Response.StatusCode);
    }

    [Fact]
    public async Task HandleAsync_CreatesNewGroup_WhenGroupIdNotInDatabase()
    {
        // Arrange – database has no groups yet
        _pluginGroupService.GetAllPluginGroups().Returns([]);
        var newGroupId = Guid.NewGuid();
        var request = new UpdatePluginLayoutRequest
        {
            Groups = [new PluginLayoutGroupEntry { GroupId = newGroupId, Name = "New Group", Plugins = [] }]
        };

        // Act
        await _endpoint.HandleAsync(request, default);

        // Assert – AddPluginGroup must have been called with the correct data
        _pluginGroupService.Received(1).AddPluginGroup(
            Arg.Is<PluginGroup>(g => g.GroupId == newGroupId && g.Name == "New Group"));
    }

    [Fact]
    public async Task HandleAsync_DerviesGroupOrderFromPositionInPayload()
    {
        // Arrange – three new groups; their Order must equal their index in the list
        _pluginGroupService.GetAllPluginGroups().Returns([]);
        var idA = Guid.NewGuid();
        var idB = Guid.NewGuid();
        var idC = Guid.NewGuid();
        var capturedGroups = new List<PluginGroup>();
        _pluginGroupService
            .When(s => s.AddPluginGroup(Arg.Any<PluginGroup>()))
            .Do(ci => capturedGroups.Add(ci.Arg<PluginGroup>()));

        var request = new UpdatePluginLayoutRequest
        {
            Groups =
            [
                new PluginLayoutGroupEntry { GroupId = idA, Name = "A", Plugins = [] },
                new PluginLayoutGroupEntry { GroupId = idB, Name = "B", Plugins = [] },
                new PluginLayoutGroupEntry { GroupId = idC, Name = "C", Plugins = [] }
            ]
        };

        // Act
        await _endpoint.HandleAsync(request, default);

        // Assert – Order must match the zero-based position in the payload
        Assert.Equal(0, capturedGroups.First(g => g.GroupId == idA).Order);
        Assert.Equal(1, capturedGroups.First(g => g.GroupId == idB).Order);
        Assert.Equal(2, capturedGroups.First(g => g.GroupId == idC).Order);
    }

    [Fact]
    public async Task HandleAsync_UpdatesExistingGroup_WhenGroupIdExistsInDatabase()
    {
        // Arrange
        var groupId = Guid.NewGuid();
        _pluginGroupService.GetAllPluginGroups()
            .Returns([new PluginGroup { GroupId = groupId, Name = "Old Name" }]);
        var request = new UpdatePluginLayoutRequest
        {
            Groups = [new PluginLayoutGroupEntry { GroupId = groupId, Name = "New Name", Plugins = [] }]
        };

        // Act
        await _endpoint.HandleAsync(request, default);

        // Assert – UpdatePluginGroups is called; AddPluginGroup must NOT be called
        _pluginGroupService.Received(1).UpdatePluginGroups(
            Arg.Is<IEnumerable<PluginGroup>>(groups =>
                groups.Any(g => g.GroupId == groupId && g.Name == "New Name")));
        _pluginGroupService.DidNotReceive().AddPluginGroup(Arg.Any<PluginGroup>());
    }

    [Fact]
    public async Task HandleAsync_DeletesGroup_WhenGroupIdIsInDeletedGroupIds()
    {
        // Arrange
        var groupId = Guid.NewGuid();
        _pluginGroupService.GetAllPluginGroups().Returns([]);
        var request = new UpdatePluginLayoutRequest
        {
            Groups = [],
            DeletedGroupIds = [groupId]
        };

        // Act
        await _endpoint.HandleAsync(request, default);

        // Assert
        _pluginGroupService.Received(1).DeletePluginGroup(groupId);
    }

    [Fact]
    public async Task HandleAsync_PassesCorrectOrderAndGroupToPatchService()
    {
        // Arrange
        var groupId = Guid.NewGuid();
        _pluginGroupService.GetAllPluginGroups().Returns([]);
        var request = new UpdatePluginLayoutRequest
        {
            Groups =
            [
                new PluginLayoutGroupEntry
                {
                    GroupId = groupId,
                    Name = "G",
                    Plugins = ["plugin-first", "plugin-second"]
                }
            ]
        };
        IEnumerable<PluginOrderPatch>? capturedPatches = null;
        await _pluginService
            .PatchPluginOrdersAsync(Arg.Do<IEnumerable<PluginOrderPatch>>(p => capturedPatches = p));

        // Act
        await _endpoint.HandleAsync(request, default);

        // Assert – first plugin gets Order=0, second gets Order=1; both get the correct GroupId
        Assert.NotNull(capturedPatches);
        var patches = capturedPatches!.ToList();
        Assert.Equal(2, patches.Count);
        Assert.Contains(patches, p => p.PluginId == "plugin-first"  && p.Order == 0 && p.GroupId == groupId);
        Assert.Contains(patches, p => p.PluginId == "plugin-second" && p.Order == 1 && p.GroupId == groupId);
    }

    [Fact]
    public async Task HandleAsync_ThrowsValidationFailure_WhenPatchPluginOrdersThrowsArgumentException()
    {
        // Arrange – simulate a missing plugin in the database
        var groupId = Guid.NewGuid();
        _pluginGroupService.GetAllPluginGroups().Returns([]);
        _pluginService.PatchPluginOrdersAsync(Arg.Any<IEnumerable<PluginOrderPatch>>())
            .Returns<Task>(_ => throw new ArgumentException("Plugin not found"));
        var request = new UpdatePluginLayoutRequest
        {
            Groups = [new PluginLayoutGroupEntry { GroupId = groupId, Name = "G", Plugins = ["missing-plugin"] }]
        };

        // Act & Assert – ThrowError() in FastEndpoints raises ValidationFailureException in unit-test context
        var ex = await Assert.ThrowsAsync<ValidationFailureException>(() => _endpoint.HandleAsync(request, default));
        Assert.Contains("Plugin not found", ex.Failures!.First().ErrorMessage);
    }

    // ---------- Auto-cleanup ----------

    [Fact]
    public async Task HandleAsync_DeletesGroup_WhenGroupNotIncludedInRequest()
    {
        // Arrange – a group exists in the DB but is absent from the request payload
        var groupId = Guid.NewGuid();
        _pluginGroupService.GetAllPluginGroups()
            .Returns([new PluginGroup { GroupId = groupId, Name = "Orphan" }]);
        var request = new UpdatePluginLayoutRequest { Groups = [] };

        // Act
        await _endpoint.HandleAsync(request, default);

        // Assert – the group must be deleted since it is not in the payload
        _pluginGroupService.Received(1).DeletePluginGroup(groupId);
    }

    [Fact]
    public async Task HandleAsync_DeletesEmptyGroup_WhenGroupHasNoPlugins()
    {
        // Arrange – a group is present in the request but carries no plugin assignments
        var groupId = Guid.NewGuid();
        _pluginGroupService.GetAllPluginGroups()
            .Returns([new PluginGroup { GroupId = groupId, Name = "Empty" }]);
        var request = new UpdatePluginLayoutRequest
        {
            Groups = [new PluginLayoutGroupEntry { GroupId = groupId, Name = "Empty", Plugins = [] }]
        };

        // Act
        await _endpoint.HandleAsync(request, default);

        // Assert – empty groups must be removed
        _pluginGroupService.Received(1).DeletePluginGroup(groupId);
    }

    // ---------- Validator ----------

    [Fact]
    public void Validator_RejectsGroupWithEmptyGuid()
    {
        // Arrange
        var validator = new UpdatePluginLayoutValidator();

        // Act
        var result = validator.Validate(new UpdatePluginLayoutRequest
        {
            Groups = [new PluginLayoutGroupEntry { GroupId = Guid.Empty, Name = "G", Plugins = [] }]
        });

        // Assert
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validator_RejectsGroupWithEmptyName()
    {
        // Arrange
        var validator = new UpdatePluginLayoutValidator();

        // Act
        var result = validator.Validate(new UpdatePluginLayoutRequest
        {
            Groups = [new PluginLayoutGroupEntry { GroupId = Guid.NewGuid(), Name = string.Empty, Plugins = [] }]
        });

        // Assert
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validator_RejectsEmptyPluginIdWithinGroup()
    {
        // Arrange – a plugin entry with an empty string ID must not be accepted
        var validator = new UpdatePluginLayoutValidator();

        // Act
        var result = validator.Validate(new UpdatePluginLayoutRequest
        {
            Groups = [new PluginLayoutGroupEntry
            {
                GroupId = Guid.NewGuid(),
                Name    = "G",
                Plugins = [string.Empty]
            }]
        });

        // Assert
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validator_AcceptsValidPayload()
    {
        // Arrange
        var validator = new UpdatePluginLayoutValidator();

        // Act
        var result = validator.Validate(new UpdatePluginLayoutRequest
        {
            Groups = [new PluginLayoutGroupEntry { GroupId = Guid.NewGuid(), Name = "My Group", Plugins = ["plugin-a"] }],
            DeletedGroupIds = []
        });

        // Assert
        Assert.True(result.IsValid);
    }
}
