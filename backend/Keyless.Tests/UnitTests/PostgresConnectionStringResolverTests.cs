using FluentAssertions;
using Keyless.Infrastructure.Context;
using Npgsql;
using Xunit;

namespace Keyless.Tests.UnitTests;

public sealed class PostgresConnectionStringResolverTests
{
  [Fact]
  public void Resolve_WhenConnectionStringIsWhitespace_ThrowsInvalidOperationException()
  {
    var action = () => PostgresConnectionStringResolver.Resolve("   ");

    action.Should().Throw<InvalidOperationException>()
      .WithMessage("Connection string 'DefaultConnection' was not found.");
  }

  [Fact]
  public void Resolve_WhenConnectionStringIsAlreadyInNpgsqlFormat_ReturnsItUnchanged()
  {
    const string connectionString = "Host=localhost;Port=5432;Database=keyless;Username=postgres;Password=postgres";

    var resolvedConnectionString = PostgresConnectionStringResolver.Resolve(connectionString);

    resolvedConnectionString.Should().Be(connectionString);
  }

  [Fact]
  public void Resolve_WhenUriContainsTrustServerCertificate_MapsSupportedQueryParameters()
  {
    const string connectionString = "postgres://user:pass@localhost:5432/keyless?sslmode=Require&trust_server_certificate=true&pooling=false&minpoolsize=2&maxpoolsize=8";

    var resolvedConnectionString = PostgresConnectionStringResolver.Resolve(connectionString);
    var builder = new NpgsqlConnectionStringBuilder(resolvedConnectionString);

    builder.Host.Should().Be("localhost");
    builder.Port.Should().Be(5432);
    builder.Database.Should().Be("keyless");
    builder.Username.Should().Be("user");
    builder.Password.Should().Be("pass");
    builder.SslMode.Should().Be(SslMode.Require);
    builder.ContainsKey("Trust Server Certificate").Should().BeTrue();
    builder["Trust Server Certificate"].Should().Be(true);
    builder.Pooling.Should().BeFalse();
    builder.MinPoolSize.Should().Be(2);
    builder.MaxPoolSize.Should().Be(8);
  }

  [Fact]
  public void Resolve_WhenUriContainsMalformedQueryParameters_DoesNotThrowAndKeepsDefaults()
  {
    const string connectionString = "postgres://user:pass@localhost:5432/keyless?sslmode=not-a-real-mode&trust_server_certificate=definitely&pooling=maybe&minpoolsize=nope&maxpoolsize=still-nope";
    var defaults = new NpgsqlConnectionStringBuilder();

    var action = () => PostgresConnectionStringResolver.Resolve(connectionString);

    action.Should().NotThrow();

    var resolvedConnectionString = action();
    var builder = new NpgsqlConnectionStringBuilder(resolvedConnectionString);

    builder.SslMode.Should().Be(defaults.SslMode);
    builder["Trust Server Certificate"].Should().Be(defaults["Trust Server Certificate"]);
    builder.Pooling.Should().Be(defaults.Pooling);
    builder.MinPoolSize.Should().Be(defaults.MinPoolSize);
    builder.MaxPoolSize.Should().Be(defaults.MaxPoolSize);
  }

  [Fact]
  public void Resolve_WhenUriContainsEncodedCredentialsAndDefaultPort_UsesDecodedValues()
  {
    const string connectionString = "postgres://user%20name:pa%24%24@localhost/keyless-db";

    var resolvedConnectionString = PostgresConnectionStringResolver.Resolve(connectionString);
    var builder = new NpgsqlConnectionStringBuilder(resolvedConnectionString);

    builder.Host.Should().Be("localhost");
    builder.Port.Should().Be(5432);
    builder.Database.Should().Be("keyless-db");
    builder.Username.Should().Be("user name");
    builder.Password.Should().Be("pa$$");
  }
}