using Microsoft.Data.Sqlite;

namespace Keyless.Infrastructure.Context;

public static class SqliteConnectionStringResolver
{
    public static string ResolveApiProjectPath(string currentPath)
    {
        var currentDirectory = new DirectoryInfo(currentPath);

        while (currentDirectory is not null)
        {
            if (File.Exists(Path.Combine(currentDirectory.FullName, "Proiect_MPI.sln")))
            {
                var apiProjectPath = Path.Combine(currentDirectory.FullName, "Keyless.API");
                if (Directory.Exists(apiProjectPath))
                {
                    return apiProjectPath;
                }

                break;
            }

            currentDirectory = currentDirectory.Parent;
        }

        return currentPath;
    }

    public static string Resolve(string connectionString, string basePath)
    {
        var connectionStringBuilder = new SqliteConnectionStringBuilder(connectionString);

        if (!string.IsNullOrWhiteSpace(connectionStringBuilder.DataSource)
            && connectionStringBuilder.DataSource != ":memory:"
            && !Path.IsPathRooted(connectionStringBuilder.DataSource))
        {
            connectionStringBuilder.DataSource = Path.GetFullPath(
                Path.Combine(basePath, connectionStringBuilder.DataSource));
        }

        return connectionStringBuilder.ToString();
    }
}