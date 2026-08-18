// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using PluginHost.Api.Core.Options;
using PluginHost.Api.Shared.DTOs;
using PluginHost.API.Services;

namespace PluginHost.Api.Tests.Services;

public class SignedUrlServiceTests
{
    private const string TestSecret = "super-secret-key-that-is-long-enough-for-hmac";
    private const string TestIssuer = "pluginhost-test";

    private static SignedUrlService CreateSut(string secret = TestSecret, string issuer = TestIssuer)
    {
        var options = Options.Create(new PluginJwtOptions { Secret = secret, Issuer = issuer });
        var logger = Substitute.For<ILogger<SignedUrlService>>();
        return new SignedUrlService(options, logger);
    }

    // ---------- GenerateToken ----------

    [Fact]
    public void GenerateToken_ReturnsNonEmptyString()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var token = sut.GenerateToken(new PluginToken { PluginId = "my-plugin" });

        // Assert
        Assert.NotNull(token);
        Assert.NotEmpty(token);
    }

    [Fact]
    public void GenerateToken_ProducesDifferentTokens_ForDifferentPluginIds()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var t1 = sut.GenerateToken(new PluginToken { PluginId = "plugin-a" });
        var t2 = sut.GenerateToken(new PluginToken { PluginId = "plugin-b" });

        // Assert – PluginId is embedded in the token payload; different IDs must yield different tokens
        Assert.NotEqual(t1, t2);
    }

    [Fact]
    public void GenerateToken_ProducesDifferentToken_WhenFilenameIsIncluded()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var tokenWithFile    = sut.GenerateToken(new PluginToken { PluginId = "p", Filename = "icon.png" });
        var tokenWithoutFile = sut.GenerateToken(new PluginToken { PluginId = "p" });

        // Assert – the filename claim changes the token content
        Assert.NotEqual(tokenWithFile, tokenWithoutFile);
    }

    // ---------- ValidateJwtToken ----------

    [Fact]
    public void ValidateJwtToken_ReturnsPluginToken_ForValidToken()
    {
        // Arrange
        var sut = CreateSut();
        var raw = sut.GenerateToken(new PluginToken { PluginId = "my-plugin" });

        // Act
        var result = sut.ValidateJwtToken(raw);

        // Assert – PluginId must round-trip correctly through sign → validate
        Assert.NotNull(result);
        Assert.Equal("my-plugin", result!.PluginId);
    }

    [Fact]
    public void ValidateJwtToken_RestoresFilename_WhenPresentInToken()
    {
        // Arrange
        var sut = CreateSut();
        var raw = sut.GenerateToken(new PluginToken { PluginId = "p", Filename = "icon.png" });

        // Act
        var result = sut.ValidateJwtToken(raw);

        // Assert – the filename claim must also round-trip
        Assert.NotNull(result);
        Assert.Equal("icon.png", result!.Filename);
    }

    [Fact]
    public void ValidateJwtToken_LeavesFilenameNull_WhenNotPresentInToken()
    {
        // Arrange
        var sut = CreateSut();
        var raw = sut.GenerateToken(new PluginToken { PluginId = "p" }); // no filename

        // Act
        var result = sut.ValidateJwtToken(raw);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result!.Filename);
    }

    [Fact]
    public void ValidateJwtToken_ReturnsNull_ForRandomString()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var result = sut.ValidateJwtToken("this-is-not-a-jwt");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void ValidateJwtToken_ReturnsNull_WhenSignedWithWrongSecret()
    {
        // Arrange – one service signs, a different service (different secret) validates
        var signer    = CreateSut(secret: "super-secret-key-that-is-long-enough-for-hmac");
        var validator = CreateSut(secret: "a-completely-different-secret-value-xyz");
        var raw       = signer.GenerateToken(new PluginToken { PluginId = "p" });

        // Act
        var result = validator.ValidateJwtToken(raw);

        // Assert – tampered/mismatched secret must be rejected
        Assert.Null(result);
    }

    [Fact]
    public void ValidateJwtToken_ReturnsNull_ForEmptyString()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var result = sut.ValidateJwtToken(string.Empty);

        // Assert
        Assert.Null(result);
    }
}
