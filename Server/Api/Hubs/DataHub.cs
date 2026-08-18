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

namespace PluginHost.API.Hubs;

public class DataHub : Hub
{
    private readonly ILogger<DataHub> _logger;

    public DataHub(ILogger<DataHub> logger)
    {
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        //_logger.LogInformation("Client connected: {ConnectionId}", Context.ConnectionId);
        await Clients.All.SendAsync("ReceiveData", $"{Context.ConnectionId} joined");
    }

    [HubMethodName("SendData")]
    public async Task SendMessage(string message)
    {
        try
        {
            //_logger.LogInformation("Message received: {Message}", message);
            await Clients.All.SendAsync("ReceiveData", message);
        }
        catch (Exception ex)
        {
            _logger.LogError("An error occurred while sending the message: {ErrorMessage}", ex.Message);
            throw; // Wirf die Exception erneut, um sie im Log sichtbar zu machen
        }
    }
}