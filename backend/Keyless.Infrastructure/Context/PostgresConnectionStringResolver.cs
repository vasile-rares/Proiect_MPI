using Npgsql;

namespace Keyless.Infrastructure.Context;

public static class PostgresConnectionStringResolver
{
  public static string Resolve(string connectionString)
  {
    if (string.IsNullOrWhiteSpace(connectionString))
    {
      throw new InvalidOperationException("Connection string 'DefaultConnection' was not found.");
    }

    if (!connectionString.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase)
        && !connectionString.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
    {
      return connectionString;
    }

    var uri = new Uri(connectionString);
    var userInfoParts = uri.UserInfo.Split(':', 2, StringSplitOptions.None);
    var builder = new NpgsqlConnectionStringBuilder
    {
      Host = uri.Host,
      Port = uri.IsDefaultPort ? 5432 : uri.Port,
      Database = uri.AbsolutePath.TrimStart('/'),
      Username = userInfoParts.Length > 0 ? Uri.UnescapeDataString(userInfoParts[0]) : string.Empty,
      Password = userInfoParts.Length > 1 ? Uri.UnescapeDataString(userInfoParts[1]) : string.Empty,
    };

    foreach (var segment in uri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
    {
      var keyValue = segment.Split('=', 2, StringSplitOptions.None);
      if (keyValue.Length != 2)
      {
        continue;
      }

      var key = Uri.UnescapeDataString(keyValue[0]);
      var value = Uri.UnescapeDataString(keyValue[1]);

      switch (key.ToLowerInvariant())
      {
        case "sslmode":
          builder.SslMode = Enum.Parse<SslMode>(value, ignoreCase: true);
          break;
        case "trust_server_certificate":
        case "trust server certificate":
          break;
        case "pooling":
          builder.Pooling = bool.Parse(value);
          break;
        case "minimum pool size":
        case "minpoolsize":
          builder.MinPoolSize = int.Parse(value);
          break;
        case "maximum pool size":
        case "maxpoolsize":
          builder.MaxPoolSize = int.Parse(value);
          break;
      }
    }

    return builder.ConnectionString;
  }
}