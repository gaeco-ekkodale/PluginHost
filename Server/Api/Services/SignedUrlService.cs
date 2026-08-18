// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using PluginHost.Api.Core.Options;
using PluginHost.Api.Shared.DTOs;
using PluginHost.API.Services.Interfaces;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace PluginHost.API.Services;

/// <summary>
/// Implementation of the ISignedUrlService interface.
/// Provides functionality for generating and validating JWT tokens for plugin access.
/// </summary>
public class SignedUrlService : ISignedUrlService
{
    private readonly PluginJwtOptions _options;
    private readonly ILogger<SignedUrlService> _logger;
    private const string PLUGIN_ID_CLAIM = nameof(PluginToken.PluginId);
    private const string FILENAME_CLAIM = nameof(PluginToken.Filename);

    /// <summary>
    /// Initializes a new instance of the SignedUrlService class.
    /// </summary>
    /// <param name="options">The plugin JWT options</param>
    /// <param name="logger">The logger</param>
    public SignedUrlService(IOptions<PluginJwtOptions> options, ILogger<SignedUrlService> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public string GenerateToken(PluginToken pluginToken)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(_options.Secret);

        var claims = new List<Claim>
        {
            new Claim(PLUGIN_ID_CLAIM, pluginToken.PluginId)
        };

        // Add filename claim if it's specified
        if (!string.IsNullOrEmpty(pluginToken.Filename))
        {
            claims.Add(new Claim(FILENAME_CLAIM, pluginToken.Filename));
        }

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddHours(24),
            Issuer = _options.Issuer,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        var signedToken = tokenHandler.WriteToken(token);
        return signedToken;
    }

    /// <inheritdoc />
    public PluginToken? ValidateJwtToken(string token)
    {
        try
        {
            var key = Encoding.ASCII.GetBytes(_options.Secret);
            var tokenHandler = new JwtSecurityTokenHandler();
            tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = false,
                ValidIssuer = _options.Issuer,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            }, out var validatedToken);

            var jwtToken = (JwtSecurityToken)validatedToken;

            // Extract the plugin ID from the token claims
            var pluginId = jwtToken.Claims.FirstOrDefault(x => x.Type == PLUGIN_ID_CLAIM)?.Value;
            if (string.IsNullOrEmpty(pluginId))
            {
                _logger.LogWarning("JWT token is missing the plugin ID claim.");
                return null;
            }

            // Create a plugin token with the plugin ID
            var pluginToken = new PluginToken
            {
                PluginId = pluginId
            };

            // Extract the filename from the token claims if it exists
            var filename = jwtToken.Claims.FirstOrDefault(x => x.Type == FILENAME_CLAIM)?.Value;
            if (!string.IsNullOrEmpty(filename))
            {
                pluginToken.Filename = filename;
            }

            return pluginToken;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "JWT validation failed.");
            return null;
        }
    }
}