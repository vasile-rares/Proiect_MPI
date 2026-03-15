using Microsoft.AspNetCore.Mvc;
using MonkeyType.Application.IServices;
using MonkeyType.Shared.DTOs.Requests.User;
using MonkeyType.Shared.DTOs.Responses.User;

namespace MonkeyType.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IStatisticsGameService _statisticsGameService;

        public UserController(IUserService userService, IStatisticsGameService statisticsGameService)
        {
            _userService = userService;
            _statisticsGameService = statisticsGameService;
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetUserById(Guid id)
        {
            var user = await _userService.GetByIdAsync(id);
            if (user == null)
            {
                return NotFound("User not found.");
            }

            var userResponse = new UserResponseDTO
            {
                Username = user.Username,
                Email = user.Email,
                TestsStarted = user.TestsStarted,
                TestsCompleted = user.TestsCompleted,
                Biography = user.Biography
            };

            return Ok(userResponse);
        }

        [HttpPatch("{id}/update")]
        public async Task<IActionResult> UpdateUser(Guid id)
        {
            var user = await _userService.GetByIdAsync(id);
            if (user == null)
            {
                return NotFound("User not found.");
            }

            var existingUser = new UserUpdateRequestDTO
            {
                Username = user.Username,
                Email = user.Email,
                TestsStarted = user.TestsStarted,
                TestsCompleted = user.TestsCompleted,
                Biography = user.Biography
            };

            await _userService.UpdateAsync(existingUser);
            return Ok();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(Guid id)
        {
            var user = await _userService.GetByIdAsync(id);
            if (user == null)
            {
                return NotFound("User not found.");
            }

            await _userService.DeleteAsync(user);
            return Ok();
        }
    }
}