using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Keyless.API.Helpers;
using Keyless.Application.IServices;
using Keyless.Shared.DTOs.Requests.Common;
using Keyless.Shared.DTOs.Requests.StatisticsGame;
using Keyless.Shared.DTOs.Responses.StatisticsGame;
using System.ComponentModel.DataAnnotations;

namespace Keyless.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    //[Authorize]
    public class StatisticsGameController : ControllerBase
    {
        private readonly ILogger<StatisticsGameController> _logger;
        private readonly IStatisticsGameService _statisticsGameService;

        public StatisticsGameController(ILogger<StatisticsGameController> logger, IStatisticsGameService statisticsGameService)
        {
            _logger = logger;
            _statisticsGameService = statisticsGameService;
        }

        [HttpGet("user/{id}")]
        public async Task<IActionResult> GetStatisticsByUser(Guid id, [FromQuery] PaginationRequestDTO pagination)
        {
            if (!UserContextHelper.IsSelf(User, id))
            {
                return Forbid();
            }

            var statistics = await _statisticsGameService.GetByUserIdPagedAsync(id, pagination.PageNumber, pagination.PageSize);
            if (statistics.Items == null || !statistics.Items.Any())
            {
                return NotFound("Statistics not found.");
            }

            return Ok(statistics);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetStatisticsById(Guid id)
        {
            var statistics = await _statisticsGameService.GetByIdAsync(id);
            if (statistics == null)
            {
                return NotFound("Statistics not found.");
            }

            if (!UserContextHelper.IsSelf(User, statistics.UserId))
            {
                return Forbid();
            }

            return Ok(statistics);
        }

        [HttpPost]
        public async Task<IActionResult> AddStatistics([FromBody] StatisticsGameRequestDTO statisticsGame)
        {
            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            if (!UserContextHelper.IsSelf(User, statisticsGame.UserId))
            {
                return Forbid();
            }

            try
            {
                await _statisticsGameService.AddAsync(statisticsGame);
                return Ok("Statistics added successfully.");
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAllStatistics([FromQuery] PaginationRequestDTO pagination)
        {
            var statistics = await _statisticsGameService.GetPagedAsync(pagination.PageNumber, pagination.PageSize);
            return Ok(statistics);
        }

        [HttpGet("user/{id}/average")]
        public async Task<IActionResult> GetAverageStatisticsByUser(Guid id)
        {
            if (!UserContextHelper.IsSelf(User, id))
            {
                return Forbid();
            }

            var aggregate = await _statisticsGameService.GetAggregateByUserIdAsync(id);
            if (aggregate == null || aggregate.GamesCount == 0)
            {
                return NotFound("Statistics not found.");
            }

            var response = new UserStatsAggregateResponseDTO
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
            };

            return Ok(response);
        }

        [AllowAnonymous]
        [HttpGet("leaderboard")]
        public async Task<IActionResult> GetLeaderboard(
            [FromQuery] string scope = "daily",
            [FromQuery] int? durationInSeconds = null,
            [FromQuery] string? mode = null,
            [FromQuery, Range(1, 100)] int topN = 10)
        {
            try
            {
                var leaderboard = await _statisticsGameService.GetLeaderboardAsync(scope, durationInSeconds, mode, topN);
                return Ok(leaderboard);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}