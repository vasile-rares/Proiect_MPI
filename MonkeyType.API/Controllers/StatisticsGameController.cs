using Microsoft.AspNetCore.Mvc;
using MonkeyType.Application.IServices;
using MonkeyType.Shared.DTOs.Requests.StatisticsGame;

namespace MonkeyType.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
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
        public async Task<IActionResult> GetStatisticsByUser(Guid id)
        {
            var statistics = await _statisticsGameService.GetByUserIdAsync(id);
            if (statistics == null)
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

            return Ok(statistics);
        }

        [HttpPost]
        public async Task<IActionResult> AddStatistics([FromBody] StatisticsGameRequestDTO statisticsGame)
        {
            if(statisticsGame == null)
            {
                return BadRequest("Invalid statistics data.");
            }

            if(statisticsGame.WordsPerMinute < 0 || statisticsGame.Accuracy < 0 || statisticsGame.Accuracy > 100)
            {
                return BadRequest("Invalid statistics values.");
            }

            if(statisticsGame.DurationInSeconds <= 0)
            {
                return BadRequest("Duration must be greater than zero.");
            }

            if(string.IsNullOrEmpty(statisticsGame.Mode))
            {
                return BadRequest("Mode is required.");
            }

            if(statisticsGame.CorrectCharacters < 0 || statisticsGame.IncorrectCharacters < 0 || statisticsGame.ExtraCharacters < 0 || statisticsGame.MissedCharacters < 0)
            {
                return BadRequest("Character counts cannot be negative.");
            }

            await _statisticsGameService.AddAsync(statisticsGame);
            return Ok("Statistics added successfully.");
        }

        [HttpGet]
        public async Task<IActionResult> GetAllStatistics()
        {
            var statistics = await _statisticsGameService.GetAllAsync();
            return Ok(statistics);
        }

        [HttpGet("user/{id}/average")]
        public async Task<IActionResult> GetAverageStatisticsByUser(Guid id)
        {
            var statistics = await _statisticsGameService.GetByUserIdAsync(id);
            if (statistics == null || !statistics.Any())
            {
                return NotFound("Statistics not found.");
            }

            var averageStatistics = new
            {
                AverageWordsPerMinute = statistics.Average(s => s.WordsPerMinute),
                AverageAccuracy = statistics.Average(s => s.Accuracy),
                AverageConsistency = statistics.Average(s => s.Consistency),
                AverageDurationInSeconds = statistics.Average(s => s.DurationInSeconds)
            };

            return Ok(averageStatistics);
        }

        [HttpGet("leaderboard")]
        public async Task<IActionResult> GetLeaderboard(
            [FromQuery] string scope = "daily",
            [FromQuery] int? durationInSeconds = null,
            [FromQuery] string? mode = null,
            [FromQuery] int topN = 10)
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