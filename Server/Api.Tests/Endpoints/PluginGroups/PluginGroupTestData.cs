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

namespace PluginHost.Api.Tests.Endpoints.PluginGroups;

internal static class PluginGroupTestData
{
    public static PluginGroup Create(string name = "Default Group", int order = 0)
    {
        return new PluginGroup
        {
            GroupId = Guid.NewGuid(),
            Name = name,
            Order = order
        };
    }
}
