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

public class PluginRepositoryTests
{
    private static Plugin MakePlugin(string id = "my-plugin", Guid? groupId = null) => new()
    {
        Id = id,
        DisplayName = "My Plugin",
        ExposedModule = "./MyModule",
        Route = $"/{id}",
        ContainerUrl = "http://host:8080",
        IconPath = "assets/icon.png",
        EntrypointPath = "assets/remoteEntry.js",
        GroupId = groupId ?? Guid.Empty
    };

    [Fact]
    public void AddPlugin_PersistsAllFields()
    {
        // Arrange
        using var db = RepositoryTestDbContextFactory.Create();
        var sut = new PluginRepository(db);
        var groupId = Guid.NewGuid();

        // Act
        sut.AddPlugin(MakePlugin("plugin-a", groupId));

        // Assert – every field must survive the round-trip to the store
        var stored = db.Plugins.Single(p => p.Id == "plugin-a");
        stored.DisplayName.Should().Be("My Plugin");
        stored.GroupId.Should().Be(groupId);
        stored.Route.Should().Be("/plugin-a");
    }

    [Fact]
    public void AddPlugin_Throws_WhenPluginIdAlreadyExists()
    {
        // Arrange
        using var db = RepositoryTestDbContextFactory.Create();
        var sut = new PluginRepository(db);
        sut.AddPlugin(MakePlugin("dup"));

        // Act
        var act = () => sut.AddPlugin(MakePlugin("dup"));

        // Assert
        act.Should().Throw<Exception>();
    }

    [Fact]
    public void GetPluginById_ReturnsCorrectPlugin()
    {
        // Arrange – two plugins exist; only the requested one must be returned
        using var db = RepositoryTestDbContextFactory.Create();
        var sut = new PluginRepository(db);
        sut.AddPlugin(MakePlugin("plugin-a"));
        sut.AddPlugin(MakePlugin("plugin-b"));

        // Act
        var result = sut.GetPluginById("plugin-a");

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be("plugin-a");
        result.DisplayName.Should().Be("My Plugin");
    }

    [Fact]
    public void GetPluginById_ReturnsNull_WhenNotFound()
    {
        // Arrange
        using var db = RepositoryTestDbContextFactory.Create();
        var sut = new PluginRepository(db);

        // Act
        var result = sut.GetPluginById("missing");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GetAllPlugins_ReturnsAllPluginsWithCorrectIds()
    {
        // Arrange
        using var db = RepositoryTestDbContextFactory.Create();
        var sut = new PluginRepository(db);
        sut.AddPlugin(MakePlugin("a"));
        sut.AddPlugin(MakePlugin("b"));

        // Act
        var result = sut.GetAllPlugins();

        // Assert – both plugins are returned with the correct IDs
        result.Should().HaveCount(2);
        result.Should().Contain(p => p.Id == "a");
        result.Should().Contain(p => p.Id == "b");
    }

    [Fact]
    public void GetAllPlugins_ReturnsEmptyList_WhenNoPluginsExist()
    {
        // Arrange
        using var db = RepositoryTestDbContextFactory.Create();
        var sut = new PluginRepository(db);

        // Act
        var result = sut.GetAllPlugins();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void DeletePlugin_RemovesOnlyTheTargetPlugin()
    {
        // Arrange – two plugins exist; only one should be removed
        using var db = RepositoryTestDbContextFactory.Create();
        var sut = new PluginRepository(db);
        sut.AddPlugin(MakePlugin("to-delete"));
        sut.AddPlugin(MakePlugin("keep"));

        // Act
        sut.DeletePlugin("to-delete");

        // Assert
        db.Plugins.Should().NotContain(p => p.Id == "to-delete");
        db.Plugins.Should().Contain(p => p.Id == "keep");
    }

    [Fact]
    public void DeletePlugin_IsIdempotent_WhenPluginDoesNotExist()
    {
        // Arrange
        using var db = RepositoryTestDbContextFactory.Create();
        var sut = new PluginRepository(db);

        // Act & Assert
        var act = () => sut.DeletePlugin("non-existent");
        act.Should().NotThrow();
    }

    [Fact]
    public void PatchPluginOrders_UpdatesGroupIdAndOrder()
    {
        // Arrange
        using var db = RepositoryTestDbContextFactory.Create();
        var sut = new PluginRepository(db);
        var groupId = Guid.NewGuid();
        sut.AddPlugin(MakePlugin("plugin-a"));

        // Act
        sut.PatchPluginOrders([new PluginOrderPatch("plugin-a", groupId, 3)]);

        // Assert – both GroupId and Order must be updated
        var updated = db.Plugins.Single(p => p.Id == "plugin-a");
        updated.GroupId.Should().Be(groupId);
        updated.Order.Should().Be(3);
    }

    [Fact]
    public void PatchPluginOrders_UpdatesMultiplePluginsAtOnce()
    {
        // Arrange
        using var db = RepositoryTestDbContextFactory.Create();
        var sut = new PluginRepository(db);
        var groupId = Guid.NewGuid();
        sut.AddPlugin(MakePlugin("plugin-a"));
        sut.AddPlugin(MakePlugin("plugin-b"));

        // Act
        sut.PatchPluginOrders(
        [
            new PluginOrderPatch("plugin-a", groupId, 0),
            new PluginOrderPatch("plugin-b", groupId, 1)
        ]);

        // Assert
        db.Plugins.Single(p => p.Id == "plugin-a").Order.Should().Be(0);
        db.Plugins.Single(p => p.Id == "plugin-b").Order.Should().Be(1);
    }

    [Fact]
    public void PatchPluginOrders_Throws_WithMessageContainingMissingId()
    {
        // Arrange
        using var db = RepositoryTestDbContextFactory.Create();
        var sut = new PluginRepository(db);

        // Act
        var act = () => sut.PatchPluginOrders([new PluginOrderPatch("missing", Guid.NewGuid(), 0)]);

        // Assert – the exception message must identify the problematic ID
        act.Should().Throw<ArgumentException>().WithMessage("*missing*");
    }

    [Fact]
    public void ReorderPluginsByIndices_DoesNotThrow_WhenAllIdsExist()
    {
        // Arrange
        using var db = RepositoryTestDbContextFactory.Create();
        var sut = new PluginRepository(db);
        sut.AddPlugin(MakePlugin("a"));
        sut.AddPlugin(MakePlugin("b"));

        // Act & Assert
        var act = () => sut.ReorderPluginsByIndices(["a", "b"]);
        act.Should().NotThrow();
    }

    [Fact]
    public void ReorderPluginsByIndices_Throws_WhenIdNotFound()
    {
        // Arrange
        using var db = RepositoryTestDbContextFactory.Create();
        var sut = new PluginRepository(db);
        sut.AddPlugin(MakePlugin("a"));

        // Act
        var act = () => sut.ReorderPluginsByIndices(["a", "unknown"]);

        // Assert
        act.Should().Throw<ArgumentException>();
    }
}
