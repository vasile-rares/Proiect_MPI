using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FluentAssertions;
using Keyless.API.Controllers;
using Keyless.Application.IServices;
using Keyless.Domain.Entities;
using Keyless.Domain.Models;
using Keyless.Shared.DTOs.Requests.Common;
using Keyless.Shared.DTOs.Requests.StatisticsGame;
using Keyless.Shared.DTOs.Responses.StatisticsGame;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Keyless.Tests.UnitTests;

public sealed class StatisticsGameControllerTests
{
    [Fact]
    public async Task GetStatisticsByUser_WhenUserIsNotSelf_ReturnsForbid()
    {
        var controller = CreateController(new StubStatisticsGameService(), Guid.NewGuid());

        var result = await controller.GetStatisticsByUser(Guid.NewGuid(), new PaginationRequestDTO());

        result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task GetStatisticsByUser_WhenNoItemsExist_ReturnsNotFound()
    {
        var userId = Guid.NewGuid();
        var statisticsService = new StubStatisticsGameService
        {
            PagedByUserResult = new PagedResult<StatisticsGameResponseDTO>
            {
                Items = [],
                PageNumber = 1,
                PageSize = 10,
                TotalCount = 0
            }
        };
        var controller = CreateController(statisticsService, userId);

        var result = await controller.GetStatisticsByUser(userId, new PaginationRequestDTO { PageNumber = 1, PageSize = 10 });

        result.Should().BeOfType<NotFoundObjectResult>()
            .Which.Value.Should().Be("Statistics not found.");
    }

    [Fact]
    public async Task GetStatisticsByUser_WhenItemsExist_ReturnsOkWithPagedResult()
    {
        var userId = Guid.NewGuid();
        var expected = new PagedResult<StatisticsGameResponseDTO>
        {
            Items =
            [
                new StatisticsGameResponseDTO { Id = Guid.NewGuid(), UserId = userId, DurationInSeconds = 15, Mode = "time" }
            ],
            PageNumber = 2,
            PageSize = 5,
            TotalCount = 1
        };
        var statisticsService = new StubStatisticsGameService { PagedByUserResult = expected };
        var controller = CreateController(statisticsService, userId);

        var result = await controller.GetStatisticsByUser(userId, new PaginationRequestDTO { PageNumber = 2, PageSize = 5 });

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeSameAs(expected);
        statisticsService.LastRequestedUserId.Should().Be(userId);
        statisticsService.LastPageNumber.Should().Be(2);
        statisticsService.LastPageSize.Should().Be(5);
    }

    [Fact]
    public async Task GetStatisticsById_WhenStatisticsDoNotExist_ReturnsNotFound()
    {
        var controller = CreateController(new StubStatisticsGameService(), Guid.NewGuid());

        var result = await controller.GetStatisticsById(Guid.NewGuid());

        result.Should().BeOfType<NotFoundObjectResult>()
            .Which.Value.Should().Be("Statistics not found.");
    }

    [Fact]
    public async Task GetStatisticsById_WhenStatisticsBelongToDifferentUser_ReturnsForbid()
    {
        var authenticatedUserId = Guid.NewGuid();
        var statisticsService = new StubStatisticsGameService
        {
            SingleStatistic = new StatisticsGameResponseDTO
            {
                Id = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                DurationInSeconds = 30,
                Mode = "time"
            }
        };
        var controller = CreateController(statisticsService, authenticatedUserId);

        var result = await controller.GetStatisticsById(Guid.NewGuid());

        result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task GetStatisticsById_WhenStatisticsBelongToCurrentUser_ReturnsOk()
    {
        var userId = Guid.NewGuid();
        var statistic = new StatisticsGameResponseDTO
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            DurationInSeconds = 60,
            Mode = "time"
        };
        var controller = CreateController(new StubStatisticsGameService { SingleStatistic = statistic }, userId);

        var result = await controller.GetStatisticsById(statistic.Id);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeSameAs(statistic);
    }

    [Fact]
    public async Task AddStatistics_WhenModelStateIsInvalid_ReturnsValidationProblem()
    {
        var userId = Guid.NewGuid();
        var controller = CreateController(new StubStatisticsGameService(), userId);
        controller.ModelState.AddModelError("Mode", "Required");

        var result = await controller.AddStatistics(new StatisticsGameRequestDTO { UserId = userId });

        var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.Value.Should().BeOfType<ValidationProblemDetails>();
    }

    [Fact]
    public async Task AddStatistics_WhenUserIsNotSelf_ReturnsForbid()
    {
        var controller = CreateController(new StubStatisticsGameService(), Guid.NewGuid());

        var result = await controller.AddStatistics(new StatisticsGameRequestDTO { UserId = Guid.NewGuid() });

        result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task AddStatistics_WhenServiceThrowsArgumentException_ReturnsBadRequest()
    {
        var userId = Guid.NewGuid();
        var statisticsService = new StubStatisticsGameService
        {
            AddException = new ArgumentException("bad request")
        };
        var controller = CreateController(statisticsService, userId);

        var result = await controller.AddStatistics(new StatisticsGameRequestDTO { UserId = userId });

        result.Should().BeOfType<BadRequestObjectResult>()
            .Which.Value.Should().Be("bad request");
    }

    [Fact]
    public async Task AddStatistics_WhenServiceSucceeds_ReturnsOkAndForwardsRequest()
    {
        var userId = Guid.NewGuid();
        var request = new StatisticsGameRequestDTO
        {
            UserId = userId,
            CorrectCharacters = 250,
            IncorrectCharacters = 5,
            ExtraCharacters = 0,
            MissedCharacters = 1,
            DurationInSeconds = 30,
            Mode = "time"
        };
        var statisticsService = new StubStatisticsGameService();
        var controller = CreateController(statisticsService, userId);

        var result = await controller.AddStatistics(request);

        result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().Be("Statistics added successfully.");
        statisticsService.LastAddedRequest.Should().BeEquivalentTo(request);
    }

    [Fact]
    public async Task GetAllStatistics_ReturnsOkWithPagedStatistics()
    {
        var expected = new PagedResult<StatisticsGameResponseDTO>
        {
            Items = [new StatisticsGameResponseDTO { Id = Guid.NewGuid(), UserId = Guid.NewGuid(), DurationInSeconds = 15, Mode = "time" }],
            PageNumber = 3,
            PageSize = 20,
            TotalCount = 1
        };
        var statisticsService = new StubStatisticsGameService { PagedAllResult = expected };
        var controller = CreateController(statisticsService, Guid.NewGuid());

        var result = await controller.GetAllStatistics(new PaginationRequestDTO { PageNumber = 3, PageSize = 20 });

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeSameAs(expected);
        statisticsService.LastPageNumber.Should().Be(3);
        statisticsService.LastPageSize.Should().Be(20);
    }

    [Fact]
    public async Task GetAverageStatisticsByUser_WhenUserIsNotSelf_ReturnsForbid()
    {
        var controller = CreateController(new StubStatisticsGameService(), Guid.NewGuid());

        var result = await controller.GetAverageStatisticsByUser(Guid.NewGuid());

        result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task GetAverageStatisticsByUser_WhenAggregateDoesNotExist_ReturnsNotFound()
    {
        var userId = Guid.NewGuid();
        var controller = CreateController(new StubStatisticsGameService(), userId);

        var result = await controller.GetAverageStatisticsByUser(userId);

        result.Should().BeOfType<NotFoundObjectResult>()
            .Which.Value.Should().Be("Statistics not found.");
    }

    [Fact]
    public async Task GetAverageStatisticsByUser_WhenGamesCountIsZero_ReturnsNotFound()
    {
        var userId = Guid.NewGuid();
        var controller = CreateController(new StubStatisticsGameService
        {
            Aggregate = new UserStatsAggregate { UserId = userId, GamesCount = 0 }
        }, userId);

        var result = await controller.GetAverageStatisticsByUser(userId);

        result.Should().BeOfType<NotFoundObjectResult>()
            .Which.Value.Should().Be("Statistics not found.");
    }

    [Fact]
    public async Task GetAverageStatisticsByUser_WhenAggregateExists_ReturnsMappedResponse()
    {
        var userId = Guid.NewGuid();
        var aggregate = new UserStatsAggregate
        {
            UserId = userId,
            GamesCount = 4,
            HighestWordsPerMinute = 90m,
            AverageWordsPerMinute = 70m,
            HighestRawWordsPerMinute = 98m,
            AverageRawWordsPerMinute = 77m,
            HighestAccuracy = 99m,
            AverageAccuracy = 95m,
            HighestConsistency = 97m,
            AverageConsistency = 93m,
            UpdatedAt = DateTime.UtcNow
        };
        var controller = CreateController(new StubStatisticsGameService { Aggregate = aggregate }, userId);

        var result = await controller.GetAverageStatisticsByUser(userId);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeEquivalentTo(new UserStatsAggregateResponseDTO
        {
            UserId = aggregate.UserId,
            GamesCount = aggregate.GamesCount,
            HighestWordsPerMinute = aggregate.HighestWordsPerMinute,
            AverageWordsPerMinute = aggregate.AverageWordsPerMinute,
            HighestRawWordsPerMinute = aggregate.HighestRawWordsPerMinute,
            AverageRawWordsPerMinute = aggregate.AverageRawWordsPerMinute,
            HighestAccuracy = aggregate.HighestAccuracy,
            AverageAccuracy = aggregate.AverageAccuracy,
            HighestConsistency = aggregate.HighestConsistency,
            AverageConsistency = aggregate.AverageConsistency,
            UpdatedAt = aggregate.UpdatedAt
        });
    }

    [Fact]
    public async Task GetLeaderboard_WhenServiceThrowsArgumentException_ReturnsBadRequest()
    {
        var controller = CreateController(new StubStatisticsGameService
        {
            LeaderboardException = new ArgumentException("invalid scope")
        }, null);

        var result = await controller.GetLeaderboard("monthly", 15, "time", 10);

        result.Should().BeOfType<BadRequestObjectResult>()
            .Which.Value.Should().Be("invalid scope");
    }

    [Fact]
    public async Task GetLeaderboard_WhenServiceSucceeds_ReturnsOk()
    {
        var expected = new[]
        {
            new LeaderboardEntry
            {
                UserId = Guid.NewGuid(),
                Username = "leader",
                WordsPerMinute = 99m,
                Accuracy = 98m,
                DurationInSeconds = 15,
                Mode = "time",
                CreatedAt = DateTime.UtcNow
            }
        };
        var statisticsService = new StubStatisticsGameService { Leaderboard = expected };
        var controller = CreateController(statisticsService, null);

        var result = await controller.GetLeaderboard("daily", 15, "time", 10);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeSameAs(expected);
        statisticsService.LastLeaderboardScope.Should().Be("daily");
        statisticsService.LastLeaderboardDuration.Should().Be(15);
        statisticsService.LastLeaderboardMode.Should().Be("time");
        statisticsService.LastLeaderboardTopN.Should().Be(10);
    }

    private static StatisticsGameController CreateController(IStatisticsGameService statisticsGameService, Guid? authenticatedUserId)
    {
        return new StatisticsGameController(NullLogger<StatisticsGameController>.Instance, statisticsGameService)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = CreatePrincipal(authenticatedUserId)
                }
            }
        };
    }

    private static ClaimsPrincipal CreatePrincipal(Guid? userId)
    {
        if (userId == null)
        {
            return new ClaimsPrincipal(new ClaimsIdentity());
        }

        return new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(JwtRegisteredClaimNames.Sub, userId.Value.ToString())
        ],
        "TestAuth"));
    }

    private sealed class StubStatisticsGameService : IStatisticsGameService
    {
        public PagedResult<StatisticsGameResponseDTO> PagedByUserResult { get; set; } = new();
        public PagedResult<StatisticsGameResponseDTO> PagedAllResult { get; set; } = new();
        public StatisticsGameResponseDTO? SingleStatistic { get; set; }
        public UserStatsAggregate? Aggregate { get; set; }
        public IEnumerable<LeaderboardEntry> Leaderboard { get; set; } = [];
        public ArgumentException? AddException { get; set; }
        public ArgumentException? LeaderboardException { get; set; }
        public Guid? LastRequestedUserId { get; private set; }
        public int LastPageNumber { get; private set; }
        public int LastPageSize { get; private set; }
        public StatisticsGameRequestDTO? LastAddedRequest { get; private set; }
        public string? LastLeaderboardScope { get; private set; }
        public int? LastLeaderboardDuration { get; private set; }
        public string? LastLeaderboardMode { get; private set; }
        public int LastLeaderboardTopN { get; private set; }

        public Task<IEnumerable<StatisticsGameResponseDTO>?> GetAllAsync() => throw new NotImplementedException();

        public Task<PagedResult<StatisticsGameResponseDTO>> GetPagedAsync(int pageNumber, int pageSize)
        {
            LastPageNumber = pageNumber;
            LastPageSize = pageSize;
            return Task.FromResult(PagedAllResult);
        }

        public Task AddAsync(StatisticsGameRequestDTO statisticsGame)
        {
            LastAddedRequest = statisticsGame;
            if (AddException != null)
            {
                throw AddException;
            }

            return Task.CompletedTask;
        }

        public Task<IEnumerable<StatisticsGameResponseDTO>?> GetByUserIdAsync(Guid userId) => throw new NotImplementedException();

        public Task<PagedResult<StatisticsGameResponseDTO>> GetByUserIdPagedAsync(Guid userId, int pageNumber, int pageSize)
        {
            LastRequestedUserId = userId;
            LastPageNumber = pageNumber;
            LastPageSize = pageSize;
            return Task.FromResult(PagedByUserResult);
        }

        public Task<StatisticsGameResponseDTO?> GetByIdAsync(Guid id) => Task.FromResult(SingleStatistic);

        public Task<UserStatsAggregate?> GetAggregateByUserIdAsync(Guid userId) => Task.FromResult(Aggregate);

        public Task<IEnumerable<LeaderboardEntry>> GetLeaderboardAsync(string scope, int? durationInSeconds, string? mode, int topN)
        {
            LastLeaderboardScope = scope;
            LastLeaderboardDuration = durationInSeconds;
            LastLeaderboardMode = mode;
            LastLeaderboardTopN = topN;

            if (LeaderboardException != null)
            {
                throw LeaderboardException;
            }

            return Task.FromResult(Leaderboard);
        }
    }
}