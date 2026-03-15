using MonkeyType.Application.IServices;
using MonkeyType.Shared.DTOs.Requests.User;
using MonkeyType.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace MonkeyType.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthenticationController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IAuthenticationService _authenticationService;
        private readonly IConfiguration _configuration;
        private readonly IJwtService _jwtService;

        public AuthenticationController(
            IUserService userService,
            IAuthenticationService authenticationService,
            IConfiguration configuration,
            IJwtService jwtService)
        {
            _userService = userService;
            _authenticationService = authenticationService;
            _configuration = configuration;
            _jwtService = jwtService;
        }

        private bool ValidatePassword(string password)
        {
            if (password.Length < 8)
                return false;

            if (!password.Any(char.IsUpper) || !password.Any(char.IsLower) ||
                !password.Any(char.IsDigit) || !password.Any(ch => !char.IsLetterOrDigit(ch)))
                return false;

            return true;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequestDTO request)
        {
            if(string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.Password) || 
            string.IsNullOrEmpty(request.Email))
            {
                return BadRequest("Username, password, and email are required.");
            }

            if(!ValidatePassword(request.Password))
            {
                return BadRequest("Password must be at least 8 characters long and contain at least one uppercase letter, one lowercase letter, one digit and one special character.");
            }

            if (request.Password != request.VerifyPassword)
            {
                return BadRequest("Passwords do not match.");
            }

            if (request.Email != request.VerifyEmail)
            {
                return BadRequest("Emails do not match.");
            }

            if(request.Username.Length < 3)
            {
                return BadRequest("Username must be at least 3 characters long.");
            }

            if(!request.Email.Contains("@") || !request.Email.Contains("."))
            {
                return BadRequest("Invalid email format.");
            }

            var existingUser = await _userService.GetByUsernameAsync(request.Username);
            if (existingUser != null)
            {
                return BadRequest("Username already exists.");
            }

            var existingEmailUser = await _userService.GetByEmailAsync(request.Email);
            if (existingEmailUser != null)
            {
                return BadRequest("Email already exists.");
            }

            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = request.Username,
                Email = request.Email,
                PasswordHash = await _authenticationService.HashPasswordAsync(request.Password),
                CreatedAt = DateTime.UtcNow
            };

            await _userService.AddAsync(user);

            var tokenExpiryMinutes = _configuration.GetValue<int?>("Jwt:ExpiryMinutes") ?? 60;
            var expiresAt = DateTime.UtcNow.AddMinutes(tokenExpiryMinutes);
            var token = _jwtService.GenerateToken(user.Id, user.Username);

            return Ok(new
            {
                message = "Registration successful.",
                token,
                expiresAt
            });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDTO request)
        {
            var user = await _userService.GetByUsernameAsync(request.Username);
            if (user == null)
            {
                return BadRequest("Invalid username or password.");
            }

            if (!await _authenticationService.VerifyPasswordAsync(request.Password, user.PasswordHash))
            {
                return BadRequest("Invalid username or password.");
            }

            var tokenExpiryMinutes = _configuration.GetValue<int?>("Jwt:ExpiryMinutes") ?? 60;
            var expiresAt = DateTime.UtcNow.AddMinutes(tokenExpiryMinutes);
            var token = _jwtService.GenerateToken(user.Id, user.Username);

            return Ok(new
            {
                message = "Login successful.",
                token,
                expiresAt
            });
        }
    }
}