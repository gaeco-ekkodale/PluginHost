// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using PluginHost.API.Services;
using PluginHost.Domain.Models;
using PluginHost.Domain.Repositories;

namespace PluginHost.Api.Tests.Services;

public class PluginGroupServiceTests
{
    private readonly IPluginGroupRepository _repository;
    private readonly ILogger<PluginGroupService> _logger;
    private readonly PluginGroupService _sut;

    public PluginGroupServiceTests()
    {
        _repository = Substitute.For<IPluginGroupRepository>();
        _logger = Substitute.For<ILogger<PluginGroupService>>();
        _sut = new PluginGroupService(_repository, _logger);
    }

    // ---------- AddPluginGroup ----------

    [Fact]
    public void AddPluginGroup_ForwardsGroupToRepository()
    {
        // Arrange
        var group = new PluginGroup { GroupId = Guid.NewGuid(), Name = "Tools" };

        // Act
        _sut.AddPluginGroup(group);

        // Assert – same object must be sent to the repo without mutation
        _repository.Received(1).AddPluginGroup(group);
    }

    [Fact]
    public void AddPluginGroup_Rethrows_WhenRepositoryFails()
    {
        // Arrange
        _repository.When(r => r.AddPluginGroup(Arg.Any<PluginGroup>()))
                   .Do(_ => throw new InvalidOperationException("db error"));

        // Act & Assert – the service must not swallow exceptions
        Assert.Throws<InvalidOperationException>(
            () => _sut.AddPluginGroup(new PluginGroup { GroupId = Guid.NewGuid(), Name = "X" }));
    }

    // ---------- DeletePluginGroup ----------

    [Fact]
    public void DeletePluginGroup_ForwardsExactIdToRepository()
    {
        // Arrange
        var targetId = Guid.NewGuid();
        var otherId  = Guid.NewGuid();

        // Act
        _sut.DeletePluginGroup(targetId);

        // Assert – only the target ID reaches the repo; the other never does
        _repository.Received(1).DeletePluginGroup(targetId);
        _repository.DidNotReceive().DeletePluginGroup(otherId);
    }

    [Fact]
    public void DeletePluginGroup_Rethrows_WhenRepositoryFails()
    {
        // Arrange
        var id = Guid.NewGuid();
        _repository.When(r => r.DeletePluginGroup(id))
                   .Do(_ => throw new InvalidOperationException("db error"));

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => _sut.DeletePluginGroup(id));
    }

    // ---------- GetAllPluginGroups ----------

    [Fact]
    public void GetAllPluginGroups_ReturnsExactGroupsFromRepository()
    {
        // Arrange – repository returns two specific groups
        var groupA = new PluginGroup { GroupId = Guid.NewGuid(), Name = "A" };
        var groupB = new PluginGroup { GroupId = Guid.NewGuid(), Name = "B" };
        _repository.GetAllPluginGroups().Returns([groupA, groupB]);

        // Act
        var result = _sut.GetAllPluginGroups();

        // Assert – the exact list (not a copy) must be returned unchanged
        Assert.NotNull(result);
        Assert.Equal(2, result!.Count);
        Assert.Contains(result, g => g.GroupId == groupA.GroupId && g.Name == "A");
        Assert.Contains(result, g => g.GroupId == groupB.GroupId && g.Name == "B");
    }

    [Fact]
    public void GetAllPluginGroups_ReturnsNull_WhenRepositoryReturnsNull()
    {
        // Arrange
        _repository.GetAllPluginGroups().Returns((List<PluginGroup>?)null);

        // Act
        var result = _sut.GetAllPluginGroups();

        // Assert – null from repo must flow through to the caller
        Assert.Null(result);
    }

    [Fact]
    public void GetAllPluginGroups_ReturnsEmptyList_WhenRepositoryReturnsEmpty()
    {
        // Arrange
        _repository.GetAllPluginGroups().Returns(new List<PluginGroup>());

        // Act
        var result = _sut.GetAllPluginGroups();

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result!);
    }

    [Fact]
    public void GetAllPluginGroups_Rethrows_WhenRepositoryFails()
    {
        // Arrange
        _repository.GetAllPluginGroups().Throws(new InvalidOperationException("db error"));

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => _sut.GetAllPluginGroups());
    }

    // ---------- UpdatePluginGroups ----------

    [Fact]
    public void UpdatePluginGroups_ForwardsGroupsToRepository()
    {
        // Arrange
        var groups = new List<PluginGroup>
        {
            new() { GroupId = Guid.NewGuid(), Name = "Updated", Order = 3 }
        };

        // Act
        _sut.UpdatePluginGroups(groups);

        // Assert – same collection reference must reach the repo
        _repository.Received(1).UpdatePluginGroups(groups);
    }

    [Fact]
    public void UpdatePluginGroups_Rethrows_WhenRepositoryFails()
    {
        // Arrange
        _repository.When(r => r.UpdatePluginGroups(Arg.Any<IEnumerable<PluginGroup>>()))
                   .Do(_ => throw new InvalidOperationException("db error"));

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => _sut.UpdatePluginGroups([]));
    }
}
