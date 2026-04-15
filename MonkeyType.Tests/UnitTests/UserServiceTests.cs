using FluentAssertions;
using MonkeyType.Application.Services;
using MonkeyType.Domain.Entities;
using MonkeyType.Domain.IRepositories;
using MonkeyType.Shared.DTOs.Requests.User;
using Xunit;

namespace MonkeyType.Tests.UnitTests;

public class UserServiceTests
{
    private readonly InMemoryUserRepository _repo = new();

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
