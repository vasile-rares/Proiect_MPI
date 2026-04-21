using FluentAssertions;
using MonkeyType.Application.Services;
using MonkeyType.Domain.Entities;
using MonkeyType.Domain.IRepositories;
using MonkeyType.Domain.Models;
using MonkeyType.Shared.DTOs.Requests.StatisticsGame;
using Xunit;

namespace MonkeyType.Tests.UnitTests;

public class StatisticsGameServiceTests
{
    private readonly InMemoryStatisticsGameRepository _statsRepo = new();
    private readonly InMemoryUserRepository _userRepo = new();
    private readonly InMemoryUserStatsAggregateRepository _aggRepo = new();

    [Fact]
    public async Task AddAsync_ComputesMetrics_AndUpdatesAggregate()
    {
        var service = new StatisticsGameService(_statsRepo, _userRepo, _aggRepo);
        var userId = Guid.NewGuid();
        await _userRepo.AddAsync(new User { Id = userId, Username = "tester", Email = "t@test.com", PasswordHash = "hash", CreatedAt = DateTime.UtcNow });

        var request = new StatisticsGameRequestDTO
        {
            UserId = userId,
            CorrectCharacters = 250,
            IncorrectCharacters = 50,
            ExtraCharacters = 0,
            MissedCharacters = 0,
            DurationInSeconds = 60,
            Mode = "time"
        };

        await service.AddAsync(request);

        var stored = (await _statsRepo.GetByUserIdAsync(userId))!.First();
        stored.WordsPerMinute.Should().BeApproximately(50m, 0.001m);
        stored.RawWordsPerMinute.Should().BeApproximately(60m, 0.001m);
        stored.Accuracy.Should().BeApproximately(83.3333m, 0.001m);
        stored.Consistency.Should().BeApproximately(83.3333m, 0.001m);

        var aggregate = await _aggRepo.GetByUserIdAsync(userId);
        aggregate.Should().NotBeNull();
        aggregate!.GamesCount.Should().Be(1);
        aggregate.HighestWordsPerMinute.Should().BeApproximately(50m, 0.001m);
        aggregate.AverageWordsPerMinute.Should().BeApproximately(50m, 0.001m);
        aggregate.HighestAccuracy.Should().BeApproximately(83.3333m, 0.001m);
        aggregate.AverageAccuracy.Should().BeApproximately(83.3333m, 0.001m);
    }

    [Fact]
    public async Task AddAsync_WhenUserMissing_ThrowsArgumentException()
    {
        var service = new StatisticsGameService(_statsRepo, _userRepo, _aggRepo);

        var request = new StatisticsGameRequestDTO
        {
            UserId = Guid.NewGuid(),
            CorrectCharacters = 10,
            IncorrectCharacters = 0,
            ExtraCharacters = 0,
            MissedCharacters = 0,
            DurationInSeconds = 30,
            Mode = "time"
        };

        await Assert.ThrowsAsync<ArgumentException>(() => service.AddAsync(request));
    }

    private sealed class InMemoryStatisticsGameRepository : IStatisticsGameRepository
    {
        private readonly List<StatisticsGame> _items = new();

        public Task AddAsync(StatisticsGame statisticsGame)
        {
            _items.Add(statisticsGame);
            return Task.CompletedTask;
        }

        public Task<IEnumerable<StatisticsGame>?> GetAllAsync() => Task.FromResult<IEnumerable<StatisticsGame>?>(_items);

        public Task<StatisticsGame?> GetByIdAsync(Guid id) => Task.FromResult(_items.FirstOrDefault(x => x.Id == id));

        public Task<IEnumerable<StatisticsGame>?> GetByUserIdAsync(Guid userId) => Task.FromResult<IEnumerable<StatisticsGame>?>(_items.Where(x => x.UserId == userId).ToList());

        public Task<PagedResult<StatisticsGame>> GetByUserIdPagedAsync(Guid userId, int pageNumber, int pageSize) => throw new NotImplementedException();

        public Task<PagedResult<StatisticsGame>> GetPagedAsync(int pageNumber, int pageSize) => throw new NotImplementedException();

        public Task<IEnumerable<LeaderboardEntry>> GetLeaderboardAsync(DateTime? startUtc, DateTime? endUtc, int? durationInSeconds, string? mode, int topN) => throw new NotImplementedException();
    }

    private sealed class InMemoryUserRepository : IUserRepository
    {
        private readonly List<User> _users = new();

        public Task AddAsync(User user)
        {
            _users.Add(user);
            return Task.CompletedTask;
        }

        public Task<User?> GetByEmailAsync(string email) => Task.FromResult(_users.FirstOrDefault(u => u.Email == email));

        public Task<User?> GetByIdAsync(Guid id) => Task.FromResult(_users.FirstOrDefault(u => u.Id == id));

        public Task<User?> GetByUsernameAsync(string username) => Task.FromResult(_users.FirstOrDefault(u => u.Username == username));

        public Task UpdateAsync(User user) => Task.CompletedTask;
    }

    private sealed class InMemoryUserStatsAggregateRepository : IUserStatsAggregateRepository
    {
        private readonly Dictionary<Guid, UserStatsAggregate> _items = new();

        public Task<UserStatsAggregate?> GetByUserIdAsync(Guid userId)
        {
            _items.TryGetValue(userId, out var agg);
            return Task.FromResult(agg);
        }

        public Task UpsertAsync(UserStatsAggregate aggregate)
        {
            _items[aggregate.UserId] = aggregate;
            return Task.CompletedTask;
        }
    }
}
