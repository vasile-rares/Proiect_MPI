using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace MonkeyType.Infrastructure.Context;

public class MonkeyTypeDatabaseContextFactory : IDesignTimeDbContextFactory<MonkeyTypeDatabaseContext>
{
    public MonkeyTypeDatabaseContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<MonkeyTypeDatabaseContext>();
        optionsBuilder.UseSqlite("Data Source=MonkeyType.db");

        return new MonkeyTypeDatabaseContext(optionsBuilder.Options);
    }
}