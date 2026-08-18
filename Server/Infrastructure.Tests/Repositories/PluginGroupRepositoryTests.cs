// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

using FluentAssertions;
using PluginHost.Domain.Models;
using PluginHost.Infrastructure.Repositories;

namespace PluginHost.Infrastructure.Tests.Repositories;

public class PluginGroupRepositoryTests
{
    [Fact]
    public void AddPluginGroup_PersistsAllFields()
    {
        // Arrange
        using var db = RepositoryTestDbContextFactory.Create();
        var sut = new PluginGroupRepository(db);
        var id = Guid.NewGuid();

        // Act
        sut.AddPluginGroup(new PluginGroup { GroupId = id, Name = "Tools", Order = 3 });

        // Assert – the entity is present in the store with all fields intact
        var stored = db.PluginGroups.Single(g => g.GroupId == id);
        stored.Name.Should().Be("Tools");
        stored.Order.Should().Be(3);
    }

    [Fact]
    public void AddPluginGroup_Throws_WhenGroupIdAlreadyExists()
    {
        // Arrange
        using var db = RepositoryTestDbContextFactory.Create();
        var sut = new PluginGroupRepository(db);
        var id = Guid.NewGuid();
        sut.AddPluginGroup(new PluginGroup { GroupId = id, Name = "First" });

        // Act
        var act = () => sut.AddPluginGroup(new PluginGroup { GroupId = id, Name = "Duplicate" });

        // Assert
        act.Should().Throw<Exception>();
    }

    [Fact]
    public void GetPluginGroupById_ReturnsCorrectGroup()
    {
        // Arrange
        using var db = RepositoryTestDbContextFactory.Create();
        var sut = new PluginGroupRepository(db);
        var targetId = Guid.NewGuid();
        var otherId = Guid.NewGuid();
        sut.AddPluginGroup(new PluginGroup { GroupId = targetId, Name = "Target" });
        sut.AddPluginGroup(new PluginGroup { GroupId = otherId, Name = "Other" });

        // Act
        var result = sut.GetPluginGroupById(targetId);

        // Assert – the correct group is returned, not the other one
        result.Should().NotBeNull();
        result!.GroupId.Should().Be(targetId);
        result.Name.Should().Be("Target");
    }

    [Fact]
    public void GetPluginGroupById_ReturnsNull_WhenNotFound()
    {
        // Arrange
        using var db = RepositoryTestDbContextFactory.Create();
        var sut = new PluginGroupRepository(db);

        // Act
        var result = sut.GetPluginGroupById(Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GetAllPluginGroups_ReturnsAllGroupsWithCorrectData()
    {
        // Arrange
        using var db = RepositoryTestDbContextFactory.Create();
        var sut = new PluginGroupRepository(db);
        var idA = Guid.NewGuid();
        var idB = Guid.NewGuid();
        sut.AddPluginGroup(new PluginGroup { GroupId = idA, Name = "A", Order = 1 });
        sut.AddPluginGroup(new PluginGroup { GroupId = idB, Name = "B", Order = 2 });

        // Act
        var result = sut.GetAllPluginGroups();

        // Assert – both groups are returned with the correct names and IDs
        result.Should().HaveCount(2);
        result.Should().Contain(g => g.GroupId == idA && g.Name == "A");
        result.Should().Contain(g => g.GroupId == idB && g.Name == "B");
    }

    [Fact]
    public void GetAllPluginGroups_ReturnsEmptyList_WhenNoGroupsExist()
    {
        // Arrange
        using var db = RepositoryTestDbContextFactory.Create();
        var sut = new PluginGroupRepository(db);

        // Act
        var result = sut.GetAllPluginGroups();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void DeletePluginGroup_RemovesOnlyTheTargetGroup()
    {
        // Arrange
        using var db = RepositoryTestDbContextFactory.Create();
        var sut = new PluginGroupRepository(db);
        var deleteId = Guid.NewGuid();
        var keepId = Guid.NewGuid();
        sut.AddPluginGroup(new PluginGroup { GroupId = deleteId, Name = "ToDelete" });
        sut.AddPluginGroup(new PluginGroup { GroupId = keepId, Name = "Keep" });

        // Act
        sut.DeletePluginGroup(deleteId);

        // Assert – only the deleted group is gone; the other must still be present
        db.PluginGroups.Should().NotContain(g => g.GroupId == deleteId);
        db.PluginGroups.Should().Contain(g => g.GroupId == keepId);
    }

    [Fact]
    public void DeletePluginGroup_ReassignsPluginsToFallbackGroup_WhenNoOtherGroupExists()
    {
        // Arrange
        using var db = RepositoryTestDbContextFactory.Create();
        var groupId = Guid.NewGuid();
        db.PluginGroups.Add(new PluginGroup { GroupId = groupId, Name = "G" });
        db.Plugins.Add(new Plugin
        {
            Id = "my-plugin",
            DisplayName = "My Plugin",
            ExposedModule = "./M",
            Route = "/p",
            ContainerUrl = "http://host",
            IconPath = "icon.png",
            EntrypointPath = "entry.js",
            GroupId = groupId
        });
        db.SaveChanges();
        var sut = new PluginGroupRepository(db);

        // Act
        sut.DeletePluginGroup(groupId);

        // Assert – the plugin remains in the DB and is moved to a new fallback group
        var plugin = db.Plugins.Single(p => p.Id == "my-plugin");
        plugin.GroupId.Should().NotBe(groupId);
        plugin.GroupId.Should().NotBe(Guid.Empty);
        db.PluginGroups.Should().Contain(g => g.GroupId == plugin.GroupId && g.Name == "Neue Plugins");
    }

    [Fact]
    public void DeletePluginGroup_IsIdempotent_WhenGroupDoesNotExist()
    {
        // Arrange
        using var db = RepositoryTestDbContextFactory.Create();
        var sut = new PluginGroupRepository(db);

        // Act & Assert – calling delete for a non-existent ID must not throw
        var act = () => sut.DeletePluginGroup(Guid.NewGuid());
        act.Should().NotThrow();
    }

    [Fact]
    public void UpdatePluginGroups_OverwritesNameAndOrder()
    {
        // Arrange
        using var db = RepositoryTestDbContextFactory.Create();
        var sut = new PluginGroupRepository(db);
        var id = Guid.NewGuid();
        sut.AddPluginGroup(new PluginGroup { GroupId = id, Name = "Old", Order = 0 });

        // Act
        sut.UpdatePluginGroups([new PluginGroup { GroupId = id, Name = "New", Order = 5 }]);

        // Assert
        var updated = db.PluginGroups.Single(g => g.GroupId == id);
        updated.Name.Should().Be("New");
        updated.Order.Should().Be(5);
    }

    [Fact]
    public void UpdatePluginGroups_OnlyUpdatesRequestedGroups()
    {
        // Arrange – two groups; only one is sent in the update
        using var db = RepositoryTestDbContextFactory.Create();
        var sut = new PluginGroupRepository(db);
        var idA = Guid.NewGuid();
        var idB = Guid.NewGuid();
        sut.AddPluginGroup(new PluginGroup { GroupId = idA, Name = "A", Order = 0 });
        sut.AddPluginGroup(new PluginGroup { GroupId = idB, Name = "B", Order = 1 });

        // Act – only update group A
        sut.UpdatePluginGroups([new PluginGroup { GroupId = idA, Name = "A-Updated", Order = 10 }]);

        // Assert – B must remain untouched
        db.PluginGroups.Single(g => g.GroupId == idB).Name.Should().Be("B");
        db.PluginGroups.Single(g => g.GroupId == idB).Order.Should().Be(1);
    }

    [Fact]
    public void UpdatePluginGroups_IsIdempotent_WhenGroupNotInDatabase()
    {
        // Arrange – the group to update does not exist yet
        using var db = RepositoryTestDbContextFactory.Create();
        var sut = new PluginGroupRepository(db);

        // Act & Assert – must not throw and must not create a new entry
        var act = () => sut.UpdatePluginGroups([new PluginGroup { GroupId = Guid.NewGuid(), Name = "Ghost", Order = 1 }]);
        act.Should().NotThrow();
        db.PluginGroups.Should().BeEmpty();
    }
}
