// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

using System.Reflection;
using FastEndpoints;
using PluginHost.Api.Shared.Mappers;
using PluginHost.API.Services.Interfaces;
using NSubstitute;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;

namespace PluginHost.Api.Tests.Endpoints.Plugins;

/// <summary>
/// Injects a <see cref="PluginMapper"/> into an endpoint that uses it as a mapper field.
/// PluginMapper depends on ISignedUrlService, IHostEnvironment, and IHttpContextAccessor.
/// For unit tests we mock those and set the mapper via reflection, just as the AppOrchestrator
/// tests did for their mappers.
/// </summary>
internal static class PluginEndpointTestHelper
{
    public static PluginMapper CreateMapper(IHttpContextAccessor? httpContextAccessor = null)
    {
        var signedUrlService = Substitute.For<ISignedUrlService>();
        signedUrlService.GenerateToken(Arg.Any<Shared.DTOs.PluginToken>()).Returns("signed-token");

        var hostEnvironment = Substitute.For<IHostEnvironment>();
        hostEnvironment.EnvironmentName.Returns("Development");

        if (httpContextAccessor is null)
        {
            httpContextAccessor = Substitute.For<IHttpContextAccessor>();
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Host = new HostString("localhost", 5000);
            httpContextAccessor.HttpContext.Returns(httpContext);
        }

        return new PluginMapper(signedUrlService, hostEnvironment, httpContextAccessor);
    }

    public static void InjectMapper<TEndpoint>(TEndpoint endpoint, PluginMapper mapper)
    {
        var currentType = endpoint!.GetType();
        while (currentType is not null)
        {
            var field = currentType
                .GetFields(BindingFlags.Instance | BindingFlags.NonPublic)
                .FirstOrDefault(f => f.FieldType == typeof(PluginMapper));

            if (field is not null)
            {
                field.SetValue(endpoint, mapper);
                return;
            }

            currentType = currentType.BaseType;
        }

        throw new InvalidOperationException($"Could not inject PluginMapper into {typeof(TEndpoint).Name}.");
    }
}
