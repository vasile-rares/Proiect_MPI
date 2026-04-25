using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Keyless.Domain.Entities;
using Keyless.Infrastructure.Context;
using Keyless.Infrastructure.Repositories;
using Xunit;

namespace Keyless.Tests.UnitTests;

public sealed class UserStatsAggregateRepositoryTests
{
    private readonly DbContextOptions<KeylessDatabaseContext> _options;

    public UserStatsAggregateRepositoryTests()
    {
        _options = new DbContextOptionsBuilder<KeylessDatabaseContext>()
            .UseInMemoryDatabase($"user-stats-aggregate-tests-{Guid.NewGuid()}")
            .Options;

        using var context = CreateContext();
        context.Database.EnsureDeleted();
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

    [Fact]
    public async Task GetByUserIdAsync_WhenAggregateDoesNotExist_ReturnsNull()
    {
        await using var context = CreateContext();
        var repository = new UserStatsAggregateRepository(context);

        var aggregate = await repository.GetByUserIdAsync(Guid.NewGuid());

        aggregate.Should().BeNull();
    }

    [Fact]
    public async Task UpsertAsync_WhenAggregateDoesNotExist_InsertsNewRow()
    {
        var userId = Guid.NewGuid();

        await using (var seedContext = CreateContext())
        {
            seedContext.Users.Add(new User
            {
                Id = userId,
                Username = "new-aggregate-user",
                Email = "new-aggregate@test.com",
                PasswordHash = "hash",
                CreatedAt = DateTime.UtcNow
            });

            await seedContext.SaveChangesAsync();
        }

        await using (var context = CreateContext())
        {
            var repository = new UserStatsAggregateRepository(context);
            await repository.UpsertAsync(CreateAggregate(userId, 1, 55m, 66m, 92m, 94m));
        }

        await using var assertContext = CreateContext();
        var aggregate = await assertContext.UserStatsAggregates.SingleAsync(x => x.UserId == userId);

        aggregate.GamesCount.Should().Be(1);
        aggregate.HighestWordsPerMinute.Should().Be(55m);
        aggregate.AverageWordsPerMinute.Should().Be(55m);
        aggregate.HighestRawWordsPerMinute.Should().Be(66m);
        aggregate.AverageRawWordsPerMinute.Should().Be(66m);
        aggregate.HighestAccuracy.Should().Be(92m);
        aggregate.AverageAccuracy.Should().Be(92m);
        aggregate.HighestConsistency.Should().Be(94m);
        aggregate.AverageConsistency.Should().Be(94m);
    }

    [Fact]
    public async Task GetByUserIdAsync_WhenAggregateExists_ReturnsTrackedEntityForReadModifyUpsertFlow()
    {
        var userId = Guid.NewGuid();

        await using (var seedContext = CreateContext())
        {
            seedContext.Users.Add(new User
            {
                Id = userId,
                Username = "tracked-aggregate-user",
                Email = "tracked-aggregate@test.com",
                PasswordHash = "hash",
                CreatedAt = DateTime.UtcNow
            });

            seedContext.UserStatsAggregates.Add(CreateAggregate(userId, 1, 50m, 60m, 91m, 93m));
            await seedContext.SaveChangesAsync();
        }

        await using (var context = CreateContext())
        {
            var repository = new UserStatsAggregateRepository(context);
            var aggregate = await repository.GetByUserIdAsync(userId);

            aggregate.Should().NotBeNull();
            var trackedAggregate = aggregate ?? throw new InvalidOperationException("Aggregate should exist.");

            context.Entry(trackedAggregate).State.Should().Be(EntityState.Unchanged);

            trackedAggregate.GamesCount = 2;
            trackedAggregate.HighestWordsPerMinute = 72m;
            trackedAggregate.AverageWordsPerMinute = 61m;
            trackedAggregate.HighestRawWordsPerMinute = 80m;
            trackedAggregate.AverageRawWordsPerMinute = 70m;
            trackedAggregate.HighestAccuracy = 97m;
            trackedAggregate.AverageAccuracy = 94m;
            trackedAggregate.HighestConsistency = 98m;
            trackedAggregate.AverageConsistency = 95m;
            trackedAggregate.UpdatedAt = DateTime.UtcNow;

            await repository.UpsertAsync(trackedAggregate);
        }

        await using var assertContext = CreateContext();
        var aggregates = await assertContext.UserStatsAggregates.Where(x => x.UserId == userId).ToListAsync();

        aggregates.Should().HaveCount(1);
        aggregates[0].GamesCount.Should().Be(2);
        aggregates[0].HighestWordsPerMinute.Should().Be(72m);
        aggregates[0].AverageWordsPerMinute.Should().Be(61m);
        aggregates[0].HighestRawWordsPerMinute.Should().Be(80m);
        aggregates[0].AverageRawWordsPerMinute.Should().Be(70m);
        aggregates[0].HighestAccuracy.Should().Be(97m);
        aggregates[0].AverageAccuracy.Should().Be(94m);
        aggregates[0].HighestConsistency.Should().Be(98m);
        aggregates[0].AverageConsistency.Should().Be(95m);
    }

    [Fact]
    public async Task UpsertAsync_WhenAggregateWasLoadedDetached_DoesNotInsertDuplicateRow()
    {
        var userId = Guid.NewGuid();

        await using (var seedContext = CreateContext())
        {
            seedContext.Users.Add(new User
            {
                Id = userId,
                Username = "detached-aggregate-user",
                Email = "detached-aggregate@test.com",
                PasswordHash = "hash",
                CreatedAt = DateTime.UtcNow
            });

            seedContext.UserStatsAggregates.Add(CreateAggregate(userId, 1, 50m, 60m, 91m, 93m));
            await seedContext.SaveChangesAsync();
        }

        UserStatsAggregate detachedAggregate;
        await using (var readContext = CreateContext())
        {
            detachedAggregate = await readContext.UserStatsAggregates
                .AsNoTracking()
                .FirstAsync(x => x.UserId == userId);
        }

        detachedAggregate.GamesCount = 2;
        detachedAggregate.HighestWordsPerMinute = 74m;
        detachedAggregate.AverageWordsPerMinute = 62m;
        detachedAggregate.HighestRawWordsPerMinute = 82m;
        detachedAggregate.AverageRawWordsPerMinute = 71m;
        detachedAggregate.HighestAccuracy = 98m;
        detachedAggregate.AverageAccuracy = 95m;
        detachedAggregate.HighestConsistency = 99m;
        detachedAggregate.AverageConsistency = 96m;
        detachedAggregate.UpdatedAt = DateTime.UtcNow;

        await using (var writeContext = CreateContext())
        {
            var repository = new UserStatsAggregateRepository(writeContext);
            await repository.UpsertAsync(detachedAggregate);
        }

        await using var assertContext = CreateContext();
        var aggregates = await assertContext.UserStatsAggregates.Where(x => x.UserId == userId).ToListAsync();

        aggregates.Should().HaveCount(1);
        aggregates[0].GamesCount.Should().Be(2);
        aggregates[0].HighestWordsPerMinute.Should().Be(74m);
        aggregates[0].AverageWordsPerMinute.Should().Be(62m);
        aggregates[0].HighestRawWordsPerMinute.Should().Be(82m);
        aggregates[0].AverageRawWordsPerMinute.Should().Be(71m);
        aggregates[0].HighestAccuracy.Should().Be(98m);
        aggregates[0].AverageAccuracy.Should().Be(95m);
        aggregates[0].HighestConsistency.Should().Be(99m);
        aggregates[0].AverageConsistency.Should().Be(96m);
    }

    [Fact]
    public async Task UpsertAsync_WhenCalledRepeatedlyForSameEntity_PreservesSingleRowAndLatestValues()
    {
        var userId = Guid.NewGuid();

        await using (var seedContext = CreateContext())
        {
            seedContext.Users.Add(new User
            {
                Id = userId,
                Username = "repeat-upsert-user",
                Email = "repeat-upsert@test.com",
                PasswordHash = "hash",
                CreatedAt = DateTime.UtcNow
            });

            await seedContext.SaveChangesAsync();
        }

        await using (var context = CreateContext())
        {
            var repository = new UserStatsAggregateRepository(context);

            await repository.UpsertAsync(CreateAggregate(userId, 1, 50m, 60m, 91m, 93m));
            await repository.UpsertAsync(CreateAggregate(userId, 2, 72m, 81m, 96m, 97m));
            await repository.UpsertAsync(CreateAggregate(userId, 3, 75m, 84m, 98m, 99m));
        }

        await using var assertContext = CreateContext();
        var aggregates = await assertContext.UserStatsAggregates.Where(x => x.UserId == userId).ToListAsync();

        aggregates.Should().HaveCount(1);
        aggregates[0].UserId.Should().Be(userId);
        aggregates[0].GamesCount.Should().Be(3);
        aggregates[0].HighestWordsPerMinute.Should().Be(75m);
        aggregates[0].AverageWordsPerMinute.Should().Be(75m);
        aggregates[0].HighestRawWordsPerMinute.Should().Be(84m);
        aggregates[0].AverageRawWordsPerMinute.Should().Be(84m);
        aggregates[0].HighestAccuracy.Should().Be(98m);
        aggregates[0].AverageAccuracy.Should().Be(98m);
        aggregates[0].HighestConsistency.Should().Be(99m);
        aggregates[0].AverageConsistency.Should().Be(99m);
    }

    private KeylessDatabaseContext CreateContext()
    {
        return new KeylessDatabaseContext(_options);
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