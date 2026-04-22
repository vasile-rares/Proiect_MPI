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

      switch (NormalizeKey(key))
      {
        case "sslmode":
          if (Enum.TryParse<SslMode>(value, ignoreCase: true, out var sslMode))
          {
            builder.SslMode = sslMode;
          }
          break;
        case "trust_server_certificate":
        case "trustservercertificate":
          if (TryParseBoolean(value, out var trustServerCertificate))
          {
            builder["Trust Server Certificate"] = trustServerCertificate;
          }
          break;
        case "pooling":
          if (TryParseBoolean(value, out var pooling))
          {
            builder.Pooling = pooling;
          }
          break;
        case "minimum pool size":
        case "minimumpoolsize":
        case "minpoolsize":
          if (int.TryParse(value, out var minPoolSize))
          {
            builder.MinPoolSize = minPoolSize;
          }
          break;
        case "maximum pool size":
        case "maximumpoolsize":
        case "maxpoolsize":
          if (int.TryParse(value, out var maxPoolSize))
          {
            builder.MaxPoolSize = maxPoolSize;
          }
          break;
      }
    }

    return builder.ConnectionString;
  }

  private static string NormalizeKey(string key)
  {
    return key
      .Replace("_", string.Empty, StringComparison.Ordinal)
      .Replace(" ", string.Empty, StringComparison.Ordinal)
      .ToLowerInvariant();
  }

  private static bool TryParseBoolean(string value, out bool result)
  {
    if (bool.TryParse(value, out result))
    {
      return true;
    }

    switch (value.Trim())
    {
      case "1":
      case "yes":
      case "on":
        result = true;
        return true;
      case "0":
      case "no":
      case "off":
        result = false;
        return true;
      default:
        result = default;
        return false;
    }
  }
}