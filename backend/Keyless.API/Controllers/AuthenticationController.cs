using Keyless.Application.IServices;
using Keyless.Shared.DTOs.Requests.User;
using Keyless.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Keyless.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    //[Authorize]
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

        [AllowAnonymous]
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequestDTO request)
        {
            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            if(!ValidatePassword(request.Password))
            {
                return BadRequest("Password must be at least 8 characters long and contain at least one uppercase letter, one lowercase letter, one digit and one special character.");
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

        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDTO request)
        {
            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

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