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
using PluginHost.Domain.Models;

namespace PluginHost.Api.Tests.Endpoints.PluginGroups;

public class AddPluginGroupEndpointTests
{
    private readonly IPluginGroupService _pluginGroupService;
    private readonly AddPluginGroup _endpoint;

    public AddPluginGroupEndpointTests()
    {
        _pluginGroupService = Substitute.For<IPluginGroupService>();
        _endpoint = Factory.Create<AddPluginGroup>(_pluginGroupService);
    }

    [Fact]
    public async Task HandleAsync_ReturnsOk_WhenRequestIsValid()
    {
        // Arrange
        var request = new AddPluginGroupRequest { Name = "My Group" };

        // Act
        await _endpoint.HandleAsync(request, default);

        // Assert
        Assert.Equal(200, _endpoint.HttpContext.Response.StatusCode);
    }

    [Fact]
    public async Task HandleAsync_ForwardsCorrectPluginGroupDataToService()
    {
        // Arrange – capture the PluginGroup passed to the service
        PluginGroup? captured = null;
        _pluginGroupService
            .When(s => s.AddPluginGroup(Arg.Any<PluginGroup>()))
            .Do(ci => captured = ci.Arg<PluginGroup>());

        // Act
        await _endpoint.HandleAsync(new AddPluginGroupRequest { Name = "My Group" }, default);

        // Assert – the service receives the name unchanged and a freshly generated, non-empty GUID
        Assert.NotNull(captured);
        Assert.Equal("My Group", captured!.Name);
        Assert.NotEqual(Guid.Empty, captured.GroupId);
    }

    [Fact]
    public async Task HandleAsync_PropagatesException_WhenServiceFails()
    {
        // Arrange
        _pluginGroupService
            .When(s => s.AddPluginGroup(Arg.Any<PluginGroup>()))
            .Do(_ => throw new InvalidOperationException("db error"));

        // Act & Assert – the endpoint must not swallow the exception
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _endpoint.HandleAsync(new AddPluginGroupRequest { Name = "Group" }, default));
    }

    // ---------- Validator ----------

    [Fact]
    public void Validator_RejectsEmptyName()
    {
        // Arrange
        var validator = new AddPluginGroupValidator();

        // Act
        var result = validator.Validate(new AddPluginGroupRequest { Name = string.Empty });

        // Assert
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validator_RejectsWhitespaceOnlyName()
    {
        // Arrange
        var validator = new AddPluginGroupValidator();

        // Act – a name that is only spaces should be treated as missing
        var result = validator.Validate(new AddPluginGroupRequest { Name = "   " });

        // Assert
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validator_RejectsNameExceedingMaxLength()
    {
        // Arrange
        var validator = new AddPluginGroupValidator();

        // Act
        var result = validator.Validate(new AddPluginGroupRequest { Name = new string('x', 201) });

        // Assert
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validator_AcceptsNameAtExactMaxLength()
    {
        // Arrange – 200 chars is the boundary; must still be accepted
        var validator = new AddPluginGroupValidator();

        // Act
        var result = validator.Validate(new AddPluginGroupRequest { Name = new string('x', 200) });

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validator_AcceptsValidName()
    {
        // Arrange
        var validator = new AddPluginGroupValidator();

        // Act
        var result = validator.Validate(new AddPluginGroupRequest { Name = "Valid Name" });

        // Assert
        Assert.True(result.IsValid);
    }
}
