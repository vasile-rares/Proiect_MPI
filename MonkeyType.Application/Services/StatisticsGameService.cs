using MonkeyType.Domain.Entities;
using MonkeyType.Domain.IRepositories;
using MonkeyType.Application.IServices;
using MonkeyType.Domain.Models;
using MonkeyType.Shared.DTOs.Requests.StatisticsGame;

namespace MonkeyType.Application.Services
{
    public class StatisticsGameService : IStatisticsGameService
    {
        private readonly IStatisticsGameRepository _statisticsGameRepository;

        public StatisticsGameService(IStatisticsGameRepository statisticsGameRepository)
        {
            _statisticsGameRepository = statisticsGameRepository;
        }

        public async Task<IEnumerable<StatisticsGame>?> GetAllAsync()
        {
            return await _statisticsGameRepository.GetAllAsync();
        }

        public async Task AddAsync(StatisticsGameRequestDTO statisticsGame)
        {
            var newStatisticsGame = new StatisticsGame
            {
                Id = Guid.NewGuid(),
                UserId = statisticsGame.UserId,
                WordsPerMinute = statisticsGame.WordsPerMinute,
                RawWordsPerMinute = statisticsGame.RawWordsPerMinute,
                Accuracy = statisticsGame.Accuracy,
                Consistency = statisticsGame.Consistency,
                CorrectCharacters = statisticsGame.CorrectCharacters,
                IncorrectCharacters = statisticsGame.IncorrectCharacters,
                ExtraCharacters = statisticsGame.ExtraCharacters,
                MissedCharacters = statisticsGame.MissedCharacters,
                DurationInSeconds = statisticsGame.DurationInSeconds,
                Mode = statisticsGame.Mode,
                CreatedAt = DateTime.UtcNow
            };

            await _statisticsGameRepository.AddAsync(newStatisticsGame);
        }

        public async Task<IEnumerable<StatisticsGame>?> GetByUserIdAsync(Guid userId)
        {
            return await _statisticsGameRepository.GetByUserIdAsync(userId);
        }

        public async Task<StatisticsGame?> GetByIdAsync(Guid id)
        {
            return await _statisticsGameRepository.GetByIdAsync(id);
        }

        public async Task<IEnumerable<LeaderboardEntry>> GetLeaderboardAsync(string scope, int? durationInSeconds, string? mode, int topN)
        {
            var normalizedScope = scope?.Trim().ToLowerInvariant();
            DateTime? startUtc = null;
            DateTime? endUtc = null;

            switch (normalizedScope)
            {
                case "daily":
                    startUtc = DateTime.UtcNow.Date;
                    endUtc = startUtc.Value.AddDays(1);
                    break;
                case "weekly":
                    startUtc = DateTime.UtcNow.Date.AddDays(-7);
                    endUtc = null;
                    break;
                case "all-time":
                    startUtc = null;
                    endUtc = null;
                    break;
                default:
                    throw new ArgumentException("Scope must be daily, weekly, or all-time.");
            }

            return await _statisticsGameRepository.GetLeaderboardAsync(startUtc, endUtc, durationInSeconds, mode, topN);
        }
    }
}