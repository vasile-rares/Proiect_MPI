using Microsoft.AspNetCore.Mvc;
using MonkeyType.Application.Services;
using MonkeyType.Domain.IRepositories;
using MonkeyType.Application.IServices;

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

            return Ok(user);
        }
    }
}