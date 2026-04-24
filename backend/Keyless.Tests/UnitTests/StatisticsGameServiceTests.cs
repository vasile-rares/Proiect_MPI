using FluentAssertions;
using Keyless.Application.Services;
using Keyless.Domain.Entities;
using Keyless.Domain.IRepositories;
using Keyless.Domain.Models;
using Keyless.Shared.DTOs.Requests.StatisticsGame;
using Xunit;

namespace Keyless.Tests.UnitTests;

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

    [Fact]
    public async Task AddAsync_WhenRequestHasEmptyUserId_ThrowsArgumentException()
    {
        var service = new StatisticsGameService(_statsRepo, _userRepo, _aggRepo);

        var action = () => service.AddAsync(CreateRequest(Guid.Empty));

        await action.Should().ThrowAsync<ArgumentException>()
            .WithMessage("UserId is required.");
    }

    [Fact]
    public async Task AddAsync_WhenDurationIsNotPositive_ThrowsArgumentException()
    {
        var service = new StatisticsGameService(_statsRepo, _userRepo, _aggRepo);
        var userId = await SeedUserAsync();

        var action = () => service.AddAsync(CreateRequest(userId, durationInSeconds: 0));

        await action.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Duration must be greater than zero seconds.");
    }

    [Fact]
    public async Task AddAsync_WhenModeIsMissing_ThrowsArgumentException()
    {
        var service = new StatisticsGameService(_statsRepo, _userRepo, _aggRepo);
        var userId = await SeedUserAsync();

        var action = () => service.AddAsync(CreateRequest(userId, mode: "   "));

        await action.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Mode is required.");
    }

    [Fact]
    public async Task AddAsync_WhenCharacterCountsContainNegativeValue_ThrowsArgumentException()
    {
        var service = new StatisticsGameService(_statsRepo, _userRepo, _aggRepo);
        var userId = await SeedUserAsync();

        var action = () => service.AddAsync(CreateRequest(userId, incorrectCharacters: -1));

        await action.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Character counts cannot be negative.");
    }

    [Fact]
    public async Task AddAsync_WhenAllCharacterCountsAreZero_ThrowsArgumentException()
    {
        var service = new StatisticsGameService(_statsRepo, _userRepo, _aggRepo);
        var userId = await SeedUserAsync();

        var action = () => service.AddAsync(CreateRequest(
            userId,
            correctCharacters: 0,
            incorrectCharacters: 0,
            extraCharacters: 0,
            missedCharacters: 0));

        await action.Should().ThrowAsync<ArgumentException>()
            .WithMessage("At least one character measurement is required.");
    }

    [Fact]
    public async Task AddAsync_WhenModeContainsInvalidCharacters_ThrowsArgumentException()
    {
        var service = new StatisticsGameService(_statsRepo, _userRepo, _aggRepo);
        var userId = await SeedUserAsync();

        var action = () => service.AddAsync(CreateRequest(userId, mode: "time mode"));

        await action.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Mode must be alphanumeric with optional '-' or '_'.");
    }

    [Fact]
    public async Task AddAsync_WhenCalledMultipleTimes_UpdatesAggregateAveragesAndHighs()
    {
        var service = new StatisticsGameService(_statsRepo, _userRepo, _aggRepo);
        var userId = await SeedUserAsync();

        await service.AddAsync(CreateRequest(
            userId,
            correctCharacters: 250,
            incorrectCharacters: 50,
            extraCharacters: 0,
            missedCharacters: 0,
            durationInSeconds: 60,
            mode: "time"));

        await service.AddAsync(CreateRequest(
            userId,
            correctCharacters: 300,
            incorrectCharacters: 0,
            extraCharacters: 0,
            missedCharacters: 0,
            durationInSeconds: 60,
            mode: "time"));

        var aggregate = await _aggRepo.GetByUserIdAsync(userId);

        aggregate.Should().NotBeNull();
        aggregate!.GamesCount.Should().Be(2);
        aggregate.HighestWordsPerMinute.Should().BeApproximately(60m, 0.001m);
        aggregate.AverageWordsPerMinute.Should().BeApproximately(55m, 0.001m);
        aggregate.HighestRawWordsPerMinute.Should().BeApproximately(60m, 0.001m);
        aggregate.AverageRawWordsPerMinute.Should().BeApproximately(60m, 0.001m);
        aggregate.HighestAccuracy.Should().BeApproximately(100m, 0.001m);
        aggregate.AverageAccuracy.Should().BeApproximately(91.6667m, 0.001m);
        aggregate.HighestConsistency.Should().BeApproximately(100m, 0.001m);
        aggregate.AverageConsistency.Should().BeApproximately(91.6667m, 0.001m);
    }

    [Fact]
    public async Task GetAllAsync_MapsStoredEntitiesToResponseDtos()
    {
        var service = new StatisticsGameService(_statsRepo, _userRepo, _aggRepo);
        var userId = await SeedUserAsync();

        await service.AddAsync(CreateRequest(userId, mode: "time_15"));

        var items = (await service.GetAllAsync())!.ToList();

        items.Should().HaveCount(1);
        items[0].UserId.Should().Be(userId);
        items[0].Mode.Should().Be("time_15");
        items[0].CorrectCharacters.Should().Be(250);
        items[0].DurationInSeconds.Should().Be(60);
    }

    [Fact]
    public async Task GetByUserIdAsync_FiltersItemsForRequestedUser()
    {
        var service = new StatisticsGameService(_statsRepo, _userRepo, _aggRepo);
        var firstUserId = await SeedUserAsync("first-user", "first@test.com");
        var secondUserId = await SeedUserAsync("second-user", "second@test.com");

        await service.AddAsync(CreateRequest(firstUserId, mode: "time"));
        await service.AddAsync(CreateRequest(secondUserId, mode: "zen"));

        var items = (await service.GetByUserIdAsync(secondUserId))!.ToList();

        items.Should().HaveCount(1);
        items[0].UserId.Should().Be(secondUserId);
        items[0].Mode.Should().Be("zen");
    }

    [Fact]
    public async Task GetPagedAsync_MapsPaginationMetadataAndItems()
    {
        var service = new StatisticsGameService(_statsRepo, _userRepo, _aggRepo);
        var firstUserId = await SeedUserAsync("paged-first", "paged-first@test.com");
        var secondUserId = await SeedUserAsync("paged-second", "paged-second@test.com");

        await service.AddAsync(CreateRequest(firstUserId, mode: "time"));
        await service.AddAsync(CreateRequest(secondUserId, mode: "zen"));

        var result = await service.GetPagedAsync(2, 5);

        result.PageNumber.Should().Be(2);
        result.PageSize.Should().Be(5);
        result.TotalCount.Should().Be(2);
        result.Items.Should().HaveCount(2);
        result.Items.Select(item => item.Mode).Should().Contain(["time", "zen"]);
    }

    [Fact]
    public async Task GetByUserIdPagedAsync_MapsFilteredPaginationMetadataAndItems()
    {
        var service = new StatisticsGameService(_statsRepo, _userRepo, _aggRepo);
        var targetUserId = await SeedUserAsync("paged-user", "paged-user@test.com");
        var otherUserId = await SeedUserAsync("other-user", "other-user@test.com");

        await service.AddAsync(CreateRequest(targetUserId, mode: "time"));
        await service.AddAsync(CreateRequest(targetUserId, mode: "zen"));
        await service.AddAsync(CreateRequest(otherUserId, mode: "practice"));

        var result = await service.GetByUserIdPagedAsync(targetUserId, 1, 10);

        result.PageNumber.Should().Be(1);
        result.PageSize.Should().Be(10);
        result.TotalCount.Should().Be(2);
        result.Items.Should().HaveCount(2);
        result.Items.Select(item => item.UserId).Should().OnlyContain(id => id == targetUserId);
    }

    [Fact]
    public async Task GetByIdAsync_WhenEntityExists_MapsItToResponseDto()
    {
        var service = new StatisticsGameService(_statsRepo, _userRepo, _aggRepo);
        var userId = await SeedUserAsync();

        await service.AddAsync(CreateRequest(userId));
        var storedEntity = (await _statsRepo.GetByUserIdAsync(userId))!.Single();

        var result = await service.GetByIdAsync(storedEntity.Id);

        result.Should().NotBeNull();
        result!.Id.Should().Be(storedEntity.Id);
        result.WordsPerMinute.Should().BeApproximately(storedEntity.WordsPerMinute, 0.001m);
        result.RawWordsPerMinute.Should().BeApproximately(storedEntity.RawWordsPerMinute, 0.001m);
        result.Accuracy.Should().BeApproximately(storedEntity.Accuracy, 0.001m);
        result.Consistency.Should().BeApproximately(storedEntity.Consistency, 0.001m);
    }

    [Fact]
    public async Task GetByIdAsync_WhenEntityDoesNotExist_ReturnsNull()
    {
        var service = new StatisticsGameService(_statsRepo, _userRepo, _aggRepo);

        var result = await service.GetByIdAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAggregateByUserIdAsync_ReturnsStoredAggregate()
    {
        var service = new StatisticsGameService(_statsRepo, _userRepo, _aggRepo);
        var userId = await SeedUserAsync();

        await service.AddAsync(CreateRequest(userId));

        var aggregate = await service.GetAggregateByUserIdAsync(userId);

        aggregate.Should().NotBeNull();
        aggregate!.UserId.Should().Be(userId);
        aggregate.GamesCount.Should().Be(1);
    }

    [Fact]
    public async Task GetLeaderboardAsync_ForwardsFiltersToRepository()
    {
        var leaderboardEntries = new[]
        {
            new LeaderboardEntry
            {
                UserId = Guid.NewGuid(),
                Username = "leader",
                WordsPerMinute = 88m,
                Accuracy = 98m,
                DurationInSeconds = 30,
                Mode = "time",
                CreatedAt = DateTime.UtcNow
            }
        };
        _statsRepo.LeaderboardResult = leaderboardEntries;
        var service = new StatisticsGameService(_statsRepo, _userRepo, _aggRepo);

        var result = (await service.GetLeaderboardAsync("all-time", 30, "time", 5)).ToList();

        result.Should().ContainSingle().Which.Username.Should().Be("leader");
        _statsRepo.LastLeaderboardStartUtc.Should().BeNull();
        _statsRepo.LastLeaderboardEndUtc.Should().BeNull();
        _statsRepo.LastLeaderboardDuration.Should().Be(30);
        _statsRepo.LastLeaderboardMode.Should().Be("time");
        _statsRepo.LastLeaderboardTopN.Should().Be(5);
    }

    [Fact]
    public async Task GetLeaderboardAsync_TrimsAndNormalizesScope()
    {
        var service = new StatisticsGameService(_statsRepo, _userRepo, _aggRepo);

        await service.GetLeaderboardAsync("  ALL-TIME  ", null, null, 3);

        _statsRepo.LastLeaderboardStartUtc.Should().BeNull();
        _statsRepo.LastLeaderboardEndUtc.Should().BeNull();
        _statsRepo.LastLeaderboardTopN.Should().Be(3);
    }

    [Theory]
    [InlineData("daily")]
    [InlineData("weekly")]
    public async Task GetLeaderboardAsync_ForTimeScopedQueries_SetsExpectedDateRange(string scope)
    {
        var service = new StatisticsGameService(_statsRepo, _userRepo, _aggRepo);

        await service.GetLeaderboardAsync(scope, null, null, 10);

        _statsRepo.LastLeaderboardStartUtc.Should().NotBeNull();
        if (scope == "daily")
        {
            _statsRepo.LastLeaderboardEndUtc.Should().Be(_statsRepo.LastLeaderboardStartUtc!.Value.AddDays(1));
        }
        else
        {
            _statsRepo.LastLeaderboardEndUtc.Should().BeNull();
        }
    }

    [Fact]
    public async Task GetLeaderboardAsync_WhenScopeIsInvalid_ThrowsArgumentException()
    {
        var service = new StatisticsGameService(_statsRepo, _userRepo, _aggRepo);

        var action = () => service.GetLeaderboardAsync("monthly", null, null, 10);

        await action.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Scope must be daily, weekly, or all-time.");
    }

    private async Task<Guid> SeedUserAsync(
        string username = "tester",
        string email = "t@test.com")
    {
        var userId = Guid.NewGuid();
        await _userRepo.AddAsync(new User
        {
            Id = userId,
            Username = username,
            Email = email,
            PasswordHash = "hash",
            CreatedAt = DateTime.UtcNow
        });

        return userId;
    }

    private static StatisticsGameRequestDTO CreateRequest(
        Guid userId,
        int correctCharacters = 250,
        int incorrectCharacters = 50,
        int extraCharacters = 0,
        int missedCharacters = 0,
        int durationInSeconds = 60,
        string mode = "time")
    {
        return new StatisticsGameRequestDTO
        {
            UserId = userId,
            CorrectCharacters = correctCharacters,
            IncorrectCharacters = incorrectCharacters,
            ExtraCharacters = extraCharacters,
            MissedCharacters = missedCharacters,
            DurationInSeconds = durationInSeconds,
            Mode = mode
        };
    }

    private sealed class InMemoryStatisticsGameRepository : IStatisticsGameRepository
    {
        private readonly List<StatisticsGame> _items = new();

        public IEnumerable<LeaderboardEntry> LeaderboardResult { get; set; } = [];
        public DateTime? LastLeaderboardStartUtc { get; private set; }
        public DateTime? LastLeaderboardEndUtc { get; private set; }
        public int? LastLeaderboardDuration { get; private set; }
        public string? LastLeaderboardMode { get; private set; }
        public int LastLeaderboardTopN { get; private set; }

        public Task AddAsync(StatisticsGame statisticsGame)
        {
            _items.Add(statisticsGame);
            return Task.CompletedTask;
        }

        public Task<IEnumerable<StatisticsGame>?> GetAllAsync() => Task.FromResult<IEnumerable<StatisticsGame>?>(_items);

        public Task<StatisticsGame?> GetByIdAsync(Guid id) => Task.FromResult(_items.FirstOrDefault(x => x.Id == id));

        public Task<IEnumerable<StatisticsGame>?> GetByUserIdAsync(Guid userId) => Task.FromResult<IEnumerable<StatisticsGame>?>(_items.Where(x => x.UserId == userId).ToList());

        public Task<PagedResult<StatisticsGame>> GetByUserIdPagedAsync(Guid userId, int pageNumber, int pageSize)
        {
            var items = _items.Where(x => x.UserId == userId).ToList();
            return Task.FromResult(new PagedResult<StatisticsGame>
            {
                Items = items,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = items.Count
            });
        }

        public Task<PagedResult<StatisticsGame>> GetPagedAsync(int pageNumber, int pageSize)
        {
            return Task.FromResult(new PagedResult<StatisticsGame>
            {
                Items = _items,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = _items.Count
            });
        }

        public Task<IEnumerable<LeaderboardEntry>> GetLeaderboardAsync(DateTime? startUtc, DateTime? endUtc, int? durationInSeconds, string? mode, int topN)
        {
            LastLeaderboardStartUtc = startUtc;
            LastLeaderboardEndUtc = endUtc;
            LastLeaderboardDuration = durationInSeconds;
            LastLeaderboardMode = mode;
            LastLeaderboardTopN = topN;
            return Task.FromResult(LeaderboardResult);
        }
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
