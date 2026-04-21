using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using MonkeyType.Application.IServices;
using MonkeyType.Application.Services;
using MonkeyType.Domain.Entities;
using MonkeyType.Domain.IRepositories;
using Xunit;

namespace MonkeyType.Tests.UnitTests;

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
}
