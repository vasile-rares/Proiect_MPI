using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Keyless.API.Helpers;
using Keyless.Application.IServices;
using Keyless.Shared.DTOs.Requests.User;
using Keyless.Shared.DTOs.Responses.User;

namespace Keyless.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    //[Authorize]
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
            if (!UserContextHelper.IsSelf(User, id))
            {
                return Forbid();
            }

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
        public async Task<IActionResult> UpdateUser(Guid id, [FromBody] UserUpdateRequestDTO request)
        {
            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            if (!UserContextHelper.IsSelf(User, id))
            {
                return Forbid();
            }

            var updated = await _userService.UpdateAsync(id, request);
            if (!updated)
            {
                return NotFound("User not found.");
            }

            return Ok();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(Guid id)
        {
            if (!UserContextHelper.IsSelf(User, id))
            {
                return Forbid();
            }

            var deleted = await _userService.DeleteAsync(id);
            if (!deleted)
            {
                return NotFound("User not found.");
            }

            return Ok();
        }
    }
}