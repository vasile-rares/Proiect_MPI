using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using System.Text.Json;

namespace Keyless.Infrastructure.Context;

public class KeylessDatabaseContextFactory : IDesignTimeDbContextFactory<KeylessDatabaseContext>
{
    public KeylessDatabaseContext CreateDbContext(string[] args)
    {
        var solutionRoot = ResolveSolutionRoot();
        var apiProjectPath = Path.Combine(solutionRoot, "Keyless.API");
        var connectionString = ReadConnectionString(apiProjectPath);

        var optionsBuilder = new DbContextOptionsBuilder<KeylessDatabaseContext>();
        optionsBuilder.UseSqlite(SqliteConnectionStringResolver.Resolve(connectionString, apiProjectPath));

        return new KeylessDatabaseContext(optionsBuilder.Options);
    }

    private static string ResolveSolutionRoot()
    {
        var currentDirectory = new DirectoryInfo(Directory.GetCurrentDirectory());

        while (currentDirectory is not null)
        {
            if (File.Exists(Path.Combine(currentDirectory.FullName, "Proiect_MPI.sln")))
            {
                return currentDirectory.FullName;
            }

            currentDirectory = currentDirectory.Parent;
        }

        throw new InvalidOperationException("Could not locate the solution root for design-time DbContext creation.");
    }

    private static string ReadConnectionString(string apiProjectPath)
    {
        var appSettingsPath = Path.Combine(apiProjectPath, "appsettings.json");
        using var document = JsonDocument.Parse(File.ReadAllText(appSettingsPath));

        if (document.RootElement.TryGetProperty("ConnectionStrings", out var connectionStrings)
            && connectionStrings.TryGetProperty("DefaultConnection", out var defaultConnection)
            && !string.IsNullOrWhiteSpace(defaultConnection.GetString()))
        {
            return defaultConnection.GetString()!;
        }

        throw new InvalidOperationException("Connection string 'DefaultConnection' was not found.");
    }
}