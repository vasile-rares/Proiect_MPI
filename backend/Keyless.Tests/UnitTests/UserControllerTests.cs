using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FluentAssertions;
using Keyless.API.Controllers;
using Keyless.Application.IServices;
using Keyless.Domain.Entities;
using Keyless.Shared.DTOs.Requests.User;
using Keyless.Shared.DTOs.Responses.User;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace Keyless.Tests.UnitTests;

public sealed class UserControllerTests
{
    [Fact]
    public async Task GetUserById_WhenUserIsNotSelf_ReturnsForbid()
    {
        var userId = Guid.NewGuid();
        var controller = CreateController(new StubUserService(), new StubStatisticsGameService(), Guid.NewGuid());

        var result = await controller.GetUserById(userId);

        result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task GetUserById_WhenUserDoesNotExist_ReturnsNotFound()
    {
        var userId = Guid.NewGuid();
        var controller = CreateController(new StubUserService(), new StubStatisticsGameService(), userId);

        var result = await controller.GetUserById(userId);

        result.Should().BeOfType<NotFoundObjectResult>()
            .Which.Value.Should().Be("User not found.");
    }

    [Fact]
    public async Task GetUserById_WhenUserExistsAndIsSelf_ReturnsMappedResponse()
    {
        var userId = Guid.NewGuid();
        var userService = new StubUserService
        {
            UserResponseById = new UserResponseDTO
            {
                Username = "tester",
                Email = "tester@example.com",
                TestsStarted = 12,
                TestsCompleted = 10,
                Biography = "bio"
            }
        };
        var controller = CreateController(userService, new StubStatisticsGameService(), userId);

        var result = await controller.GetUserById(userId);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeEquivalentTo(userService.UserResponseById);
    }

    [Fact]
    public async Task UpdateUser_WhenModelStateIsInvalid_ReturnsValidationProblem()
    {
        var userId = Guid.NewGuid();
        var controller = CreateController(new StubUserService(), new StubStatisticsGameService(), userId);
        controller.ModelState.AddModelError("Username", "Required");

        var result = await controller.UpdateUser(userId, new UserUpdateRequestDTO());

        var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.Value.Should().BeOfType<ValidationProblemDetails>();
    }

    [Fact]
    public async Task UpdateUser_WhenUserIsNotSelf_ReturnsForbid()
    {
        var controller = CreateController(new StubUserService(), new StubStatisticsGameService(), Guid.NewGuid());

        var result = await controller.UpdateUser(Guid.NewGuid(), new UserUpdateRequestDTO());

        result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task UpdateUser_WhenUserDoesNotExist_ReturnsNotFound()
    {
        var userId = Guid.NewGuid();
        var controller = CreateController(new StubUserService(), new StubStatisticsGameService(), userId);

        var result = await controller.UpdateUser(userId, new UserUpdateRequestDTO());

        result.Should().BeOfType<NotFoundObjectResult>()
            .Which.Value.Should().Be("User not found.");
    }

    [Fact]
    public async Task UpdateUser_WhenUpdateSucceeds_ReturnsOkAndForwardsRequest()
    {
        var userId = Guid.NewGuid();
        var request = new UserUpdateRequestDTO
        {
            Username = "updated",
            Email = "updated@example.com",
            TestsStarted = 7,
            TestsCompleted = 5,
            Biography = "updated bio"
        };
        var userService = new StubUserService { UpdateResult = true };
        var controller = CreateController(userService, new StubStatisticsGameService(), userId);

        var result = await controller.UpdateUser(userId, request);

        result.Should().BeOfType<OkResult>();
        userService.LastUpdatedUserId.Should().Be(userId);
        userService.LastUpdateRequest.Should().BeEquivalentTo(request);
    }

    [Fact]
    public async Task DeleteUser_WhenUserIsNotSelf_ReturnsForbid()
    {
        var controller = CreateController(new StubUserService(), new StubStatisticsGameService(), Guid.NewGuid());

        var result = await controller.DeleteUser(Guid.NewGuid());

        result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task DeleteUser_WhenUserDoesNotExist_ReturnsNotFound()
    {
        var userId = Guid.NewGuid();
        var controller = CreateController(new StubUserService(), new StubStatisticsGameService(), userId);

        var result = await controller.DeleteUser(userId);

        result.Should().BeOfType<NotFoundObjectResult>()
            .Which.Value.Should().Be("User not found.");
    }

    [Fact]
    public async Task DeleteUser_WhenDeleteSucceeds_ReturnsOk()
    {
        var userId = Guid.NewGuid();
        var userService = new StubUserService { DeleteResult = true };
        var controller = CreateController(userService, new StubStatisticsGameService(), userId);

        var result = await controller.DeleteUser(userId);

        result.Should().BeOfType<OkResult>();
        userService.LastDeletedUserId.Should().Be(userId);
    }

    private static UserController CreateController(
        IUserService userService,
        IStatisticsGameService statisticsGameService,
        Guid? authenticatedUserId = null)
    {
        var controller = new UserController(userService, statisticsGameService)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = CreatePrincipal(authenticatedUserId)
                }
            }
        };

        return controller;
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

    private sealed class StubUserService : IUserService
    {
        public UserResponseDTO? UserResponseById { get; set; }
        public User? UserByUsername { get; set; }
        public User? UserByEmail { get; set; }
        public bool UpdateResult { get; set; }
        public bool DeleteResult { get; set; }
        public Guid? LastUpdatedUserId { get; private set; }
        public UserUpdateRequestDTO? LastUpdateRequest { get; private set; }
        public Guid? LastDeletedUserId { get; private set; }

        public Task<UserResponseDTO?> GetByIdAsync(Guid id) => Task.FromResult(UserResponseById);

        public Task<User?> GetByUsernameAsync(string username) => Task.FromResult(UserByUsername);

        public Task<User?> GetByEmailAsync(string email) => Task.FromResult(UserByEmail);

        public Task AddAsync(User user) => Task.CompletedTask;

        public Task<bool> UpdateAsync(Guid id, UserUpdateRequestDTO user)
        {
            LastUpdatedUserId = id;
            LastUpdateRequest = user;
            return Task.FromResult(UpdateResult);
        }

        public Task<bool> DeleteAsync(Guid id)
        {
            LastDeletedUserId = id;
            return Task.FromResult(DeleteResult);
        }
    }

    private sealed class StubStatisticsGameService : IStatisticsGameService
    {
        public Task<IEnumerable<Keyless.Shared.DTOs.Responses.StatisticsGame.StatisticsGameResponseDTO>?> GetAllAsync() => throw new NotImplementedException();
        public Task<Keyless.Domain.Models.PagedResult<Keyless.Shared.DTOs.Responses.StatisticsGame.StatisticsGameResponseDTO>> GetPagedAsync(int pageNumber, int pageSize) => throw new NotImplementedException();
        public Task AddAsync(Keyless.Shared.DTOs.Requests.StatisticsGame.StatisticsGameRequestDTO statisticsGame) => throw new NotImplementedException();
        public Task<IEnumerable<Keyless.Shared.DTOs.Responses.StatisticsGame.StatisticsGameResponseDTO>?> GetByUserIdAsync(Guid userId) => throw new NotImplementedException();
        public Task<Keyless.Domain.Models.PagedResult<Keyless.Shared.DTOs.Responses.StatisticsGame.StatisticsGameResponseDTO>> GetByUserIdPagedAsync(Guid userId, int pageNumber, int pageSize) => throw new NotImplementedException();
        public Task<Keyless.Shared.DTOs.Responses.StatisticsGame.StatisticsGameResponseDTO?> GetByIdAsync(Guid id) => throw new NotImplementedException();
        public Task<UserStatsAggregate?> GetAggregateByUserIdAsync(Guid userId) => throw new NotImplementedException();
        public Task<IEnumerable<Keyless.Domain.Models.LeaderboardEntry>> GetLeaderboardAsync(string scope, int? durationInSeconds, string? mode, int topN) => throw new NotImplementedException();
    }
}