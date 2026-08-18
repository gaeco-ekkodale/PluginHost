// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

using PluginHost.Domain.Models;

namespace PluginHost.Api.Tests.Endpoints.Plugins;

internal static class PluginTestData
{
    public static Plugin Create(
        string pluginId = "my-plugin",
        string name = "My Plugin",
        string module = "./MyModule",
        string route = "/my-plugin",
        string containerUrl = "http://my-plugin:8080",
        string icon = "assets/icon.png",
        string entryPoint = "assets/remoteEntry.js")
    {
        return new Plugin
        {
            Id = pluginId,
            DisplayName = name,
            ExposedModule = module,
            Route = route,
            ContainerUrl = containerUrl,
            IconPath = icon,
            EntrypointPath = entryPoint,
            GroupId = Guid.NewGuid()
        };
    }
}
