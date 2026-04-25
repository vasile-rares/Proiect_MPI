using FluentAssertions;
using Keyless.API.Controllers;
using Keyless.Application.IServices;
using Keyless.Domain.Entities;
using Keyless.Shared.DTOs.Requests.User;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Keyless.Tests.UnitTests;

public sealed class AuthenticationControllerTests
{
    [Fact]
    public async Task Register_WhenModelStateIsInvalid_ReturnsValidationProblem()
    {
        var controller = CreateController(new StubUserService(), new StubAuthenticationService(), CreateConfiguration(), new StubJwtService());
        controller.ModelState.AddModelError("Username", "Required");

        var result = await controller.Register(CreateRegisterRequest());

        var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.Value.Should().BeOfType<ValidationProblemDetails>();
    }

    [Fact]
    public async Task Register_WhenPasswordIsWeak_ReturnsBadRequest()
    {
        var controller = CreateController(new StubUserService(), new StubAuthenticationService(), CreateConfiguration(), new StubJwtService());
        var request = CreateRegisterRequest(password: "weakpass", verifyPassword: "weakpass");

        var result = await controller.Register(request);

        result.Should().BeOfType<BadRequestObjectResult>()
            .Which.Value.Should().Be("Password must be at least 8 characters long and contain at least one uppercase letter, one lowercase letter, one digit and one special character.");
    }

    [Fact]
    public async Task Register_WhenUsernameAlreadyExists_ReturnsBadRequest()
    {
        var userService = new StubUserService
        {
            UserByUsername = new User { Id = Guid.NewGuid(), Username = "existing", Email = "existing@test.com", PasswordHash = "hash", CreatedAt = DateTime.UtcNow }
        };
        var controller = CreateController(userService, new StubAuthenticationService(), CreateConfiguration(), new StubJwtService());

        var result = await controller.Register(CreateRegisterRequest(username: "existing"));

        result.Should().BeOfType<BadRequestObjectResult>()
            .Which.Value.Should().Be("Username already exists.");
    }

    [Fact]
    public async Task Register_WhenEmailAlreadyExists_ReturnsBadRequest()
    {
        var userService = new StubUserService
        {
            UserByEmail = new User { Id = Guid.NewGuid(), Username = "other", Email = "existing@test.com", PasswordHash = "hash", CreatedAt = DateTime.UtcNow }
        };
        var controller = CreateController(userService, new StubAuthenticationService(), CreateConfiguration(), new StubJwtService());

        var result = await controller.Register(CreateRegisterRequest(email: "existing@test.com", verifyEmail: "existing@test.com"));

        result.Should().BeOfType<BadRequestObjectResult>()
            .Which.Value.Should().Be("Email already exists.");
    }

    [Fact]
    public async Task Register_WhenRequestIsValid_AddsUserAndReturnsTokenPayload()
    {
        var userService = new StubUserService();
        var authenticationService = new StubAuthenticationService { HashResult = "hashed-password" };
        var jwtService = new StubJwtService { TokenToReturn = "jwt-token" };
        var controller = CreateController(userService, authenticationService, CreateConfiguration(expiryMinutes: 90), jwtService);
        var request = CreateRegisterRequest();

        var result = await controller.Register(request);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ReadProperty<string>(ok.Value, "message").Should().Be("Registration successful.");
        ReadProperty<string>(ok.Value, "token").Should().Be("jwt-token");
        ReadProperty<DateTime>(ok.Value, "expiresAt").Should().BeAfter(DateTime.UtcNow.AddMinutes(89));
        userService.LastAddedUser.Should().NotBeNull();
        userService.LastAddedUser!.Username.Should().Be(request.Username);
        userService.LastAddedUser.Email.Should().Be(request.Email);
        userService.LastAddedUser.PasswordHash.Should().Be("hashed-password");
        authenticationService.LastHashedPassword.Should().Be(request.Password);
        jwtService.LastGeneratedUsername.Should().Be(request.Username);
        jwtService.LastGeneratedUserId.Should().Be(userService.LastAddedUser.Id);
    }

    [Fact]
    public async Task Login_WhenModelStateIsInvalid_ReturnsValidationProblem()
    {
        var controller = CreateController(new StubUserService(), new StubAuthenticationService(), CreateConfiguration(), new StubJwtService());
        controller.ModelState.AddModelError("Username", "Required");

        var result = await controller.Login(new LoginRequestDTO());

        var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.Value.Should().BeOfType<ValidationProblemDetails>();
    }

    [Fact]
    public async Task Login_WhenUserDoesNotExist_ReturnsBadRequest()
    {
        var controller = CreateController(new StubUserService(), new StubAuthenticationService(), CreateConfiguration(), new StubJwtService());

        var result = await controller.Login(new LoginRequestDTO { Username = "missing", Password = "Keyless!123" });

        result.Should().BeOfType<BadRequestObjectResult>()
            .Which.Value.Should().Be("Invalid username or password.");
    }

    [Fact]
    public async Task Login_WhenPasswordDoesNotMatch_ReturnsBadRequest()
    {
        var userService = new StubUserService
        {
            UserByUsername = new User { Id = Guid.NewGuid(), Username = "tester", Email = "tester@test.com", PasswordHash = "stored-hash", CreatedAt = DateTime.UtcNow }
        };
        var authenticationService = new StubAuthenticationService { VerifyResult = false };
        var controller = CreateController(userService, authenticationService, CreateConfiguration(), new StubJwtService());

        var result = await controller.Login(new LoginRequestDTO { Username = "tester", Password = "Keyless!123" });

        result.Should().BeOfType<BadRequestObjectResult>()
            .Which.Value.Should().Be("Invalid username or password.");
        authenticationService.LastVerifiedPassword.Should().Be("Keyless!123");
        authenticationService.LastVerifiedHash.Should().Be("stored-hash");
    }

    [Fact]
    public async Task Login_WhenCredentialsAreValid_ReturnsTokenPayload()
    {
        var user = new User { Id = Guid.NewGuid(), Username = "tester", Email = "tester@test.com", PasswordHash = "stored-hash", CreatedAt = DateTime.UtcNow };
        var userService = new StubUserService { UserByUsername = user };
        var authenticationService = new StubAuthenticationService { VerifyResult = true };
        var jwtService = new StubJwtService { TokenToReturn = "jwt-token" };
        var controller = CreateController(userService, authenticationService, CreateConfiguration(expiryMinutes: 75), jwtService);

        var result = await controller.Login(new LoginRequestDTO { Username = "tester", Password = "Keyless!123" });

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ReadProperty<string>(ok.Value, "message").Should().Be("Login successful.");
        ReadProperty<string>(ok.Value, "token").Should().Be("jwt-token");
        ReadProperty<DateTime>(ok.Value, "expiresAt").Should().BeAfter(DateTime.UtcNow.AddMinutes(74));
        jwtService.LastGeneratedUserId.Should().Be(user.Id);
        jwtService.LastGeneratedUsername.Should().Be(user.Username);
    }

    private static AuthenticationController CreateController(
        IUserService userService,
        IAuthenticationService authenticationService,
        IConfiguration configuration,
        IJwtService jwtService)
    {
        return new AuthenticationController(userService, authenticationService, configuration, jwtService)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };
    }

    private static IConfiguration CreateConfiguration(int expiryMinutes = 60)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                ["Jwt:ExpiryMinutes"] = expiryMinutes.ToString()
            }!)
            .Build();
    }

    private static RegisterRequestDTO CreateRegisterRequest(
        string username = "tester",
        string email = "tester@example.com",
        string verifyEmail = "tester@example.com",
        string password = "Keyless!123",
        string verifyPassword = "Keyless!123")
    {
        return new RegisterRequestDTO
        {
            Username = username,
            Email = email,
            VerifyEmail = verifyEmail,
            Password = password,
            VerifyPassword = verifyPassword
        };
    }

    private static T ReadProperty<T>(object? source, string propertyName)
    {
        source.Should().NotBeNull();
        var property = source!.GetType().GetProperty(propertyName);
        property.Should().NotBeNull();
        return (T)property!.GetValue(source)!;
    }

    private sealed class StubUserService : IUserService
    {
        public User? UserByUsername { get; set; }
        public User? UserByEmail { get; set; }
        public User? LastAddedUser { get; private set; }

        public Task<Keyless.Shared.DTOs.Responses.User.UserResponseDTO?> GetByIdAsync(Guid id) => throw new NotImplementedException();
        public Task<User?> GetByUsernameAsync(string username) => Task.FromResult(UserByUsername);
        public Task<User?> GetByEmailAsync(string email) => Task.FromResult(UserByEmail);

        public Task AddAsync(User user)
        {
            LastAddedUser = user;
            return Task.CompletedTask;
        }

        public Task<bool> UpdateAsync(Guid id, UserUpdateRequestDTO user) => throw new NotImplementedException();
        public Task<bool> DeleteAsync(Guid id) => throw new NotImplementedException();
    }

    private sealed class StubAuthenticationService : IAuthenticationService
    {
        public string HashResult { get; set; } = "hash";
        public bool VerifyResult { get; set; }
        public string? LastHashedPassword { get; private set; }
        public string? LastVerifiedPassword { get; private set; }
        public string? LastVerifiedHash { get; private set; }

        public Task<string> HashPasswordAsync(string password)
        {
            LastHashedPassword = password;
            return Task.FromResult(HashResult);
        }

        public Task<bool> VerifyPasswordAsync(string password, string passwordHash)
        {
            LastVerifiedPassword = password;
            LastVerifiedHash = passwordHash;
            return Task.FromResult(VerifyResult);
        }
    }

    private sealed class StubJwtService : IJwtService
    {
        public string TokenToReturn { get; set; } = "token";
        public Guid LastGeneratedUserId { get; private set; }
        public string? LastGeneratedUsername { get; private set; }

        public string GenerateToken(Guid userId, string username)
        {
            LastGeneratedUserId = userId;
            LastGeneratedUsername = username;
            return TokenToReturn;
        }

        public Guid? ValidateToken(string token) => throw new NotImplementedException();
    }
}