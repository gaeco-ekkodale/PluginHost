// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using PluginHost.Api.Core.Options;
using PluginHost.Api.Shared.DTOs;
using PluginHost.API.Hubs;
using PluginHost.API.Services;
using PluginHost.API.Services.Interfaces;
using PluginHost.API.Services.Interfaces.Keycloak;
using PluginHost.Domain.Models;
using PluginHost.Domain.Repositories;

namespace PluginHost.Api.Tests.Services;

public class PluginServiceTests
{
    private readonly IPluginRepository _pluginRepository;
    private readonly IPluginGroupService _pluginGroupService;
    private readonly IKeycloakHelper _keycloakHelper;
    private readonly IKeycloakClientManager _keycloakClientManager;
    private readonly ISignedUrlService _signedUrlService;
    private readonly IHubContext<DataHub> _hubContext;
    private readonly IClientProxy _clientProxy;
    private readonly PluginService _sut;

    public PluginServiceTests()
    {
        _pluginRepository = Substitute.For<IPluginRepository>();
        _pluginGroupService = Substitute.For<IPluginGroupService>();
        _keycloakHelper = Substitute.For<IKeycloakHelper>();
        _keycloakClientManager = Substitute.For<IKeycloakClientManager>();
        _signedUrlService = Substitute.For<ISignedUrlService>();

        _hubContext = Substitute.For<IHubContext<DataHub>>();
        _clientProxy = Substitute.For<IClientProxy>();
        _hubContext.Clients.All.Returns(_clientProxy);

        var signalROptions = Options.Create(new SignalROptions
        {
            PluginEvent = "PluginChange",
            Operation = new Operation
            {
                AddPlugin = "Add",
                DeletePlugin = "Delete",
                PatchPlugins = "Patch"
            }
        });

        var httpClient = new HttpClient(new NotFoundHttpMessageHandler());
        var httpClientFactory = Substitute.For<IHttpClientFactory>();
        httpClientFactory.CreateClient(Arg.Any<string>()).Returns(httpClient);
        httpClientFactory.CreateClient().Returns(httpClient);

        _sut = new PluginService(
            _pluginRepository,
            _pluginGroupService,
            _keycloakHelper,
            _keycloakClientManager,
            _signedUrlService,
            Substitute.For<ILogger<PluginService>>(),
            _hubContext,
            signalROptions,
            httpClientFactory);
    }

    private static Plugin MakePlugin(string id = "my-plugin") => new()
    {
        Id = id,
        DisplayName = "My Plugin",
        ExposedModule = "./M",
        Route = $"/{id}",
        ContainerUrl = "http://host:8080",
        IconPath = "assets/icon.png",
        EntrypointPath = "assets/remoteEntry.js",
        GroupId = Guid.NewGuid()
    };

    // ---------- RegisterPluginAsync ----------

    [Fact]
    public async Task RegisterPluginAsync_PersistsPluginWithCorrectId()
    {
        // Arrange
        _keycloakClientManager.CreateClientWithTokenExchange(Arg.Any<string>()).Returns("client-id");
        var plugin = MakePlugin("my-plugin");

        // Act
        await _sut.RegisterPluginAsync(plugin);

        // Assert – repository receives the exact plugin entity; ID must not be altered
        _pluginRepository.Received(1).AddPlugin(Arg.Is<Plugin>(p => p.Id == "my-plugin"));
    }

    [Fact]
    public async Task RegisterPluginAsync_BroadcastsAddEventViaSignalR()
    {
        // Arrange
        _keycloakClientManager.CreateClientWithTokenExchange(Arg.Any<string>()).Returns("client-id");

        // Act
        await _sut.RegisterPluginAsync(MakePlugin());

        // Assert – SignalR must notify clients with the add-operation name
        await _clientProxy.Received(1).SendCoreAsync(
            "PluginChange",
            Arg.Is<object[]>(a => HasAddEventForPlugin(a, "my-plugin")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RegisterPluginAsync_ThrowsArgumentException_WhenContainerUrlIsEmpty()
    {
        // Arrange
        var plugin = MakePlugin();
        plugin.ContainerUrl = string.Empty;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _sut.RegisterPluginAsync(plugin));
    }

    [Fact]
    public async Task RegisterPluginAsync_ThrowsArgumentException_WhenEntryPointIsEmpty()
    {
        // Arrange
        var plugin = MakePlugin();
        plugin.EntrypointPath = string.Empty;

        // Act & Assert 
        await Assert.ThrowsAsync<ArgumentException>(() => _sut.RegisterPluginAsync(plugin));
    }

    [Fact]
    public async Task RegisterPluginAsync_NeverPersists_WhenValidationFails()
    {
        // Arrange – a plugin with a missing required field must not reach the repository
        var plugin = MakePlugin();
        plugin.ContainerUrl = string.Empty;

        // Act – ignore the expected exception
        try { await _sut.RegisterPluginAsync(plugin); } catch { }

        // Assert
        _pluginRepository.DidNotReceive().AddPlugin(Arg.Any<Plugin>());
    }

    // ---------- DeletePluginAsync ----------

    [Fact]
    public async Task DeletePluginAsync_RemovesCorrectPlugin()
    {
        // Arrange
        _pluginRepository.GetPluginById("my-plugin").Returns(MakePlugin("my-plugin"));

        // Act
        await _sut.DeletePluginAsync("my-plugin");

        // Assert – repo must delete exactly the requested ID
        _pluginRepository.Received(1).DeletePlugin("my-plugin");
        _pluginRepository.DidNotReceive().DeletePlugin(Arg.Is<string>(id => id != "my-plugin"));
    }

    [Fact]
    public async Task DeletePluginAsync_BroadcastsDeleteEventViaSignalR()
    {
        // Arrange
        _pluginRepository.GetPluginById("my-plugin").Returns(MakePlugin());

        // Act
        await _sut.DeletePluginAsync("my-plugin");

        // Assert
        await _clientProxy.Received(1).SendCoreAsync(
            "PluginChange",
            Arg.Is<object[]>(a => HasDeleteEventForPlugin(a, "my-plugin")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeletePluginAsync_ThrowsFileNotFoundException_WhenPluginDoesNotExist()
    {
        // Arrange
        _pluginRepository.GetPluginById("missing").Returns((Plugin?)null);

        // Act & Assert – a non-existent plugin must produce a FileNotFoundException
        await Assert.ThrowsAsync<FileNotFoundException>(() => _sut.DeletePluginAsync("missing"));
    }

    [Fact]
    public async Task DeletePluginAsync_NeverDeletesFromRepo_WhenPluginNotFound()
    {
        // Arrange – guard: repo delete must never be called if the lookup fails
        _pluginRepository.GetPluginById("missing").Returns((Plugin?)null);

        // Act
        try { await _sut.DeletePluginAsync("missing"); } catch { }

        // Assert
        _pluginRepository.DidNotReceive().DeletePlugin(Arg.Any<string>());
    }

    // ---------- PatchPluginOrdersAsync ----------

    [Fact]
    public async Task PatchPluginOrdersAsync_ForwardsExactPatchesToRepository()
    {
        // Arrange
        var groupId = Guid.NewGuid();
        var patches = new[]
        {
            new PluginOrderPatch("plugin-a", groupId, 0),
            new PluginOrderPatch("plugin-b", groupId, 1)
        };
        IEnumerable<PluginOrderPatch>? captured = null;
        _pluginRepository.When(r => r.PatchPluginOrders(Arg.Any<IEnumerable<PluginOrderPatch>>()))
                         .Do(ci => captured = ci.Arg<IEnumerable<PluginOrderPatch>>());

        // Act
        await _sut.PatchPluginOrdersAsync(patches);

        // Assert – repo receives the exact input without transformation
        Assert.NotNull(captured);
        var list = captured!.ToList();
        Assert.Equal(2, list.Count);
        Assert.Contains(list, p => p.PluginId == "plugin-a" && p.Order == 0 && p.GroupId == groupId);
        Assert.Contains(list, p => p.PluginId == "plugin-b" && p.Order == 1 && p.GroupId == groupId);
    }

    [Fact]
    public async Task PatchPluginOrdersAsync_Rethrows_WhenRepositoryFails()
    {
        // Arrange
        _pluginRepository
            .When(r => r.PatchPluginOrders(Arg.Any<IEnumerable<PluginOrderPatch>>()))
            .Do(_ => throw new ArgumentException("Plugin not found"));

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _sut.PatchPluginOrdersAsync([new PluginOrderPatch("missing", Guid.NewGuid(), 0)]));
    }

    private static bool HasAddEventForPlugin(object[] args, string pluginId)
    {
        var evt = args.Length == 1 ? args[0] as PluginChangeEvent : null;
        return evt is not null &&
               evt.Operation == "Add" &&
               evt.AddedPluginIds.Contains(pluginId);
    }

    private static bool HasDeleteEventForPlugin(object[] args, string pluginId)
    {
        var evt = args.Length == 1 ? args[0] as PluginChangeEvent : null;
        return evt is not null &&
               evt.Operation == "Delete" &&
               evt.RemovedPluginIds.Contains(pluginId);
    }
}

/// <summary>Stub HTTP handler that always returns 404 (used to avoid real HTTP calls in unit tests).</summary>
file sealed class NotFoundHttpMessageHandler : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
        => Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.NotFound));
}
