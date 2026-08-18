// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

using System.Net;
using System.Text.Json;

namespace PluginHost.Api.Core.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var response = context.Response;
        response.ContentType = "application/json";

        var (status, message, logLevel) = GetResponse(exception);
        response.StatusCode = (int)status;

        switch (logLevel)
        {
            case LogLevel.Warning:
                _logger.LogWarning(exception, "Expected error occurred: {Message}", message);
                break;
            case LogLevel.Error:
                _logger.LogError(exception, "Unexpected error occurred: {Message}", message);
                break;
        }

        var body = JsonSerializer.Serialize(new { error = message });
        await response.WriteAsync(body);
    }

    private static (HttpStatusCode code, string message, LogLevel logLevel) GetResponse(Exception exception)
    {
        return exception switch
        {
            InvalidOperationException ex =>
                (HttpStatusCode.BadRequest, ex.Message, LogLevel.Warning),

            _ =>
                (HttpStatusCode.InternalServerError, "An unexpected error occurred.", LogLevel.Error)
        };
    }
}
