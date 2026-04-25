using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Keyless.Application.IServices;
using Keyless.Application.Services;
using System.IdentityModel.Tokens.Jwt;
using Xunit;

namespace Keyless.Tests.UnitTests;

public class AuthenticationServiceTests
{
    [Fact]
    public async Task HashAndVerifyPassword_Works()
    {
        var logger = NullLogger<JwtService>.Instance;
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                {"Jwt:Key", "0123456789abcdef0123456789abcdef"},
                {"Jwt:Issuer", "issuer"},
                {"Jwt:Audience", "aud"},
                {"Jwt:ExpiryMinutes", "60"}
            }!)
            .Build();

        IAuthenticationService auth = new AuthenticationService();
        IJwtService jwt = new JwtService(config, logger);

        var password = "P@ssw0rd!";
        var hash = await auth.HashPasswordAsync(password);
        hash.Should().NotBeNullOrEmpty();

        var ok = await auth.VerifyPasswordAsync(password, hash);
        ok.Should().BeTrue();

        var bad = await auth.VerifyPasswordAsync("wrong", hash);
        bad.Should().BeFalse();
    }

    [Fact]
    public void GenerateAndValidateToken_Works()
    {
        var logger = NullLogger<JwtService>.Instance;
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                {"Jwt:Key", "0123456789abcdef0123456789abcdef"},
                {"Jwt:Issuer", "issuer"},
                {"Jwt:Audience", "aud"},
                {"Jwt:ExpiryMinutes", "60"}
            }!)
            .Build();

        IJwtService jwt = new JwtService(config, logger);
        var userId = Guid.NewGuid();
        var token = jwt.GenerateToken(userId, "tester");

        var parsed = jwt.ValidateToken(token);
        parsed.Should().Be(userId);
    }

    [Fact]
    public void GenerateToken_IncludesUsernameClaim()
    {
        var logger = NullLogger<JwtService>.Instance;
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                {"Jwt:Key", "0123456789abcdef0123456789abcdef"},
                {"Jwt:Issuer", "issuer"},
                {"Jwt:Audience", "aud"},
                {"Jwt:ExpiryMinutes", "60"}
            }!)
            .Build();

        IJwtService jwt = new JwtService(config, logger);

        var token = jwt.GenerateToken(Guid.NewGuid(), "tester-name");
        var parsedToken = new JwtSecurityTokenHandler().ReadJwtToken(token);

        parsedToken.Claims.Single(x => x.Type == JwtRegisteredClaimNames.UniqueName).Value.Should().Be("tester-name");
    }

    [Fact]
    public void ValidateToken_WhenIssuerDoesNotMatch_ReturnsNull()
    {
        var logger = NullLogger<JwtService>.Instance;
        var sourceConfig = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                {"Jwt:Key", "0123456789abcdef0123456789abcdef"},
                {"Jwt:Issuer", "issuer-a"},
                {"Jwt:Audience", "aud"},
                {"Jwt:ExpiryMinutes", "60"}
            }!)
            .Build();
        var validationConfig = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                {"Jwt:Key", "0123456789abcdef0123456789abcdef"},
                {"Jwt:Issuer", "issuer-b"},
                {"Jwt:Audience", "aud"},
                {"Jwt:ExpiryMinutes", "60"}
            }!)
            .Build();

        var issuingJwt = new JwtService(sourceConfig, logger);
        var validatingJwt = new JwtService(validationConfig, logger);
        var token = issuingJwt.GenerateToken(Guid.NewGuid(), "tester");

        var parsed = validatingJwt.ValidateToken(token);

        parsed.Should().BeNull();
    }

    [Fact]
    public void ValidateToken_WhenTokenIsMalformed_ReturnsNull()
    {
        var logger = NullLogger<JwtService>.Instance;
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                {"Jwt:Key", "0123456789abcdef0123456789abcdef"},
                {"Jwt:Issuer", "issuer"},
                {"Jwt:Audience", "aud"},
                {"Jwt:ExpiryMinutes", "60"}
            }!)
            .Build();

        IJwtService jwt = new JwtService(config, logger);

        var parsed = jwt.ValidateToken("not-a-jwt");

        parsed.Should().BeNull();
    }

    [Fact]
    public void GenerateToken_WhenKeyIsMissing_ThrowsInvalidOperationException()
    {
        var logger = NullLogger<JwtService>.Instance;
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                {"Jwt:Issuer", "issuer"},
                {"Jwt:Audience", "aud"},
                {"Jwt:ExpiryMinutes", "60"}
            }!)
            .Build();

        IJwtService jwt = new JwtService(config, logger);

        var action = () => jwt.GenerateToken(Guid.NewGuid(), "tester");

        action.Should().Throw<InvalidOperationException>()
            .WithMessage("Jwt:Key is not configured.");
    }
}
