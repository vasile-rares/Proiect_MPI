using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using MonkeyType.Domain.Entities;
using MonkeyType.Infrastructure.Context;
using MonkeyType.Infrastructure.Repositories;
using Xunit;

namespace MonkeyType.Tests.UnitTests;

public sealed class UserStatsAggregateRepositoryTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<MonkeyTypeDatabaseContext> _options;

    public UserStatsAggregateRepositoryTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        _options = new DbContextOptionsBuilder<MonkeyTypeDatabaseContext>()
            .UseSqlite(_connection)
            .Options;

        using var context = CreateContext();
        context.Database.EnsureCreated();
    }

    [Fact]
    public async Task UpsertAsync_WhenAggregateExists_UpdatesExistingRow()
    {
        var userId = Guid.NewGuid();

        await using (var seedContext = CreateContext())
        {
            seedContext.Users.Add(new User
            {
                Id = userId,
                Username = "aggregate-user",
                Email = "aggregate@test.com",
                PasswordHash = "hash",
                CreatedAt = DateTime.UtcNow
            });

            await seedContext.SaveChangesAsync();
        }

        await using (var firstContext = CreateContext())
        {
            var repository = new UserStatsAggregateRepository(firstContext);
            await repository.UpsertAsync(CreateAggregate(userId, 1, 50m, 60m, 91m, 93m));
        }

        await using (var secondContext = CreateContext())
        {
            var repository = new UserStatsAggregateRepository(secondContext);
            await repository.UpsertAsync(CreateAggregate(userId, 2, 72m, 80m, 97m, 98m));
        }

        await using var assertContext = CreateContext();
        var aggregates = await assertContext.UserStatsAggregates.Where(x => x.UserId == userId).ToListAsync();

        aggregates.Should().HaveCount(1);
        aggregates[0].GamesCount.Should().Be(2);
        aggregates[0].HighestWordsPerMinute.Should().Be(72m);
        aggregates[0].AverageWordsPerMinute.Should().Be(72m);
        aggregates[0].HighestRawWordsPerMinute.Should().Be(80m);
        aggregates[0].AverageRawWordsPerMinute.Should().Be(80m);
        aggregates[0].HighestAccuracy.Should().Be(97m);
        aggregates[0].AverageAccuracy.Should().Be(97m);
        aggregates[0].HighestConsistency.Should().Be(98m);
        aggregates[0].AverageConsistency.Should().Be(98m);
    }

    public void Dispose()
    {
        _connection.Dispose();
    }

    private MonkeyTypeDatabaseContext CreateContext()
    {
        return new MonkeyTypeDatabaseContext(_options);
    }

    private static UserStatsAggregate CreateAggregate(Guid userId, int gamesCount, decimal highestWpm, decimal highestRawWpm, decimal highestAccuracy, decimal highestConsistency)
    {
        return new UserStatsAggregate
        {
            UserId = userId,
            GamesCount = gamesCount,
            HighestWordsPerMinute = highestWpm,
            AverageWordsPerMinute = highestWpm,
            HighestRawWordsPerMinute = highestRawWpm,
            AverageRawWordsPerMinute = highestRawWpm,
            HighestAccuracy = highestAccuracy,
            AverageAccuracy = highestAccuracy,
            HighestConsistency = highestConsistency,
            AverageConsistency = highestConsistency,
            UpdatedAt = DateTime.UtcNow
        };
    }
}