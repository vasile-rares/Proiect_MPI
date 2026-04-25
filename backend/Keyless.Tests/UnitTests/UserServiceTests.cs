using FluentAssertions;
using Keyless.Application.Services;
using Keyless.Domain.Entities;
using Keyless.Domain.IRepositories;
using Keyless.Shared.DTOs.Requests.User;
using Xunit;

namespace Keyless.Tests.UnitTests;

public class UserServiceTests
{
    private readonly InMemoryUserRepository _repo = new();

    [Fact]
    public async Task AddAsync_PersistsUserInRepository()
    {
        var service = new UserService(_repo);
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = "added-user",
            Email = "added@test.com",
            PasswordHash = "hash",
            CreatedAt = DateTime.UtcNow
        };

        await service.AddAsync(user);

        var stored = await _repo.GetByIdAsync(user.Id);
        stored.Should().BeSameAs(user);
    }

    [Fact]
    public async Task GetByIdAsync_WhenUserExists_MapsResponseDto()
    {
        var service = new UserService(_repo);
        var userId = Guid.NewGuid();
        await _repo.AddAsync(new User
        {
            Id = userId,
            Username = "mapped-user",
            Email = "mapped@test.com",
            PasswordHash = "hash",
            CreatedAt = DateTime.UtcNow,
            TestsStarted = 12,
            TestsCompleted = 10,
            Biography = "mapped bio"
        });

        var result = await service.GetByIdAsync(userId);

        result.Should().NotBeNull();
        result!.Username.Should().Be("mapped-user");
        result.Email.Should().Be("mapped@test.com");
        result.TestsStarted.Should().Be(12);
        result.TestsCompleted.Should().Be(10);
        result.Biography.Should().Be("mapped bio");
    }

    [Fact]
    public async Task GetByIdAsync_WhenUserMissing_ReturnsNull()
    {
        var service = new UserService(_repo);

        var result = await service.GetByIdAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByUsernameAsync_ReturnsMatchingUser()
    {
        var service = new UserService(_repo);
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = "lookup-user",
            Email = "lookup@test.com",
            PasswordHash = "hash",
            CreatedAt = DateTime.UtcNow
        };
        await _repo.AddAsync(user);

        var result = await service.GetByUsernameAsync("lookup-user");

        result.Should().BeSameAs(user);
    }

    [Fact]
    public async Task GetByEmailAsync_ReturnsMatchingUser()
    {
        var service = new UserService(_repo);
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = "email-user",
            Email = "email@test.com",
            PasswordHash = "hash",
            CreatedAt = DateTime.UtcNow
        };
        await _repo.AddAsync(user);

        var result = await service.GetByEmailAsync("email@test.com");

        result.Should().BeSameAs(user);
    }

    [Fact]
    public async Task DeleteAsync_ReturnsFalse_WhenUserNotFound()
    {
        var service = new UserService(_repo);

        var result = await service.DeleteAsync(Guid.NewGuid());

        result.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateAsync_ReturnsFalse_WhenUserNotFound()
    {
        var service = new UserService(_repo);
        var result = await service.UpdateAsync(Guid.NewGuid(), new UserUpdateRequestDTO { Username = "u1", Email = "e@test.com" });
        result.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateAsync_UpdatesFields_AndUpdatedAt()
    {
        var service = new UserService(_repo);
        var userId = Guid.NewGuid();
        await _repo.AddAsync(new User { Id = userId, Username = "old", Email = "old@test.com", PasswordHash = "hash", CreatedAt = DateTime.UtcNow });

        var result = await service.UpdateAsync(userId, new UserUpdateRequestDTO
        {
            Username = "newname",
            Email = "new@test.com",
            TestsStarted = 5,
            TestsCompleted = 3,
            Biography = "bio"
        });

        result.Should().BeTrue();
        var updated = await _repo.GetByIdAsync(userId);
        updated!.Username.Should().Be("newname");
        updated.Email.Should().Be("new@test.com");
        updated.TestsStarted.Should().Be(5);
        updated.TestsCompleted.Should().Be(3);
        updated.Biography.Should().Be("bio");
        updated.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task DeleteAsync_SetsDeletedAt()
    {
        var service = new UserService(_repo);
        var userId = Guid.NewGuid();
        await _repo.AddAsync(new User { Id = userId, Username = "del", Email = "del@test.com", PasswordHash = "hash", CreatedAt = DateTime.UtcNow });

        var result = await service.DeleteAsync(userId);
        result.Should().BeTrue();

        var user = await _repo.GetByIdAsync(userId);
        user!.DeletedAt.Should().NotBeNull();
    }

    private sealed class InMemoryUserRepository : IUserRepository
    {
        private readonly List<User> _users = new();

        public Task<User?> GetByIdAsync(Guid id) => Task.FromResult(_users.FirstOrDefault(u => u.Id == id));
        public Task<User?> GetByUsernameAsync(string username) => Task.FromResult(_users.FirstOrDefault(u => u.Username == username));
        public Task<User?> GetByEmailAsync(string email) => Task.FromResult(_users.FirstOrDefault(u => u.Email == email));

        public Task AddAsync(User user)
        {
            _users.Add(user);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(User user)
        {
            // In-memory list already holds reference; nothing else needed.
            return Task.CompletedTask;
        }
    }
}
