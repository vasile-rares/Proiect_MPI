using MonkeyType.Domain.Entities;
using MonkeyType.Domain.IRepositories;
using MonkeyType.Application.IServices;
using MonkeyType.Domain.Models;
using MonkeyType.Shared.DTOs.Requests.StatisticsGame;
using MonkeyType.Shared.DTOs.Responses.StatisticsGame;
using System.Text.RegularExpressions;

namespace MonkeyType.Application.Services
{
    public class StatisticsGameService : IStatisticsGameService
    {
        private readonly IStatisticsGameRepository _statisticsGameRepository;
        private readonly IUserRepository _userRepository;
        private readonly IUserStatsAggregateRepository _userStatsAggregateRepository;

        public StatisticsGameService(IStatisticsGameRepository statisticsGameRepository, IUserRepository userRepository, IUserStatsAggregateRepository userStatsAggregateRepository)
        {
            _statisticsGameRepository = statisticsGameRepository;
            _userRepository = userRepository;
            _userStatsAggregateRepository = userStatsAggregateRepository;
        }

        public async Task<IEnumerable<StatisticsGameResponseDTO>?> GetAllAsync()
        {
            var result = await _statisticsGameRepository.GetAllAsync();
            return result?.Select(MapToResponse);
        }

        public async Task<PagedResult<StatisticsGameResponseDTO>> GetPagedAsync(int pageNumber, int pageSize)
        {
            var result = await _statisticsGameRepository.GetPagedAsync(pageNumber, pageSize);
            return new PagedResult<StatisticsGameResponseDTO>
            {
                Items = result.Items.Select(MapToResponse),
                PageNumber = result.PageNumber,
                PageSize = result.PageSize,
                TotalCount = result.TotalCount
            };
        }

        public async Task AddAsync(StatisticsGameRequestDTO statisticsGame)
        {
            ValidateRequest(statisticsGame);

            var user = await _userRepository.GetByIdAsync(statisticsGame.UserId);
            if (user == null)
            {
                throw new ArgumentException("User not found.");
            }

            var metrics = CalculateDerivedStatistics(statisticsGame);

            var newStatisticsGame = new StatisticsGame
            {
                Id = Guid.NewGuid(),
                UserId = statisticsGame.UserId,
                WordsPerMinute = metrics.WordsPerMinute,
                RawWordsPerMinute = metrics.RawWordsPerMinute,
                Accuracy = metrics.Accuracy,
                Consistency = metrics.Consistency,
                CorrectCharacters = statisticsGame.CorrectCharacters,
                IncorrectCharacters = statisticsGame.IncorrectCharacters,
                ExtraCharacters = statisticsGame.ExtraCharacters,
                MissedCharacters = statisticsGame.MissedCharacters,
                DurationInSeconds = statisticsGame.DurationInSeconds,
                Mode = statisticsGame.Mode,
                CreatedAt = DateTime.UtcNow
            };

            await _statisticsGameRepository.AddAsync(newStatisticsGame);

            await UpdateAggregateAsync(statisticsGame.UserId, metrics);
        }

        public async Task<IEnumerable<StatisticsGameResponseDTO>?> GetByUserIdAsync(Guid userId)
        {
            var result = await _statisticsGameRepository.GetByUserIdAsync(userId);
            return result?.Select(MapToResponse);
        }

        public async Task<PagedResult<StatisticsGameResponseDTO>> GetByUserIdPagedAsync(Guid userId, int pageNumber, int pageSize)
        {
            var result = await _statisticsGameRepository.GetByUserIdPagedAsync(userId, pageNumber, pageSize);
            return new PagedResult<StatisticsGameResponseDTO>
            {
                Items = result.Items.Select(MapToResponse),
                PageNumber = result.PageNumber,
                PageSize = result.PageSize,
                TotalCount = result.TotalCount
            };
        }

        public async Task<StatisticsGameResponseDTO?> GetByIdAsync(Guid id)
        {
            var entity = await _statisticsGameRepository.GetByIdAsync(id);
            return entity == null ? null : MapToResponse(entity);
        }

        public async Task<UserStatsAggregate?> GetAggregateByUserIdAsync(Guid userId)
        {
            return await _userStatsAggregateRepository.GetByUserIdAsync(userId);
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

        private static void ValidateRequest(StatisticsGameRequestDTO statisticsGame)
        {
            if (statisticsGame.UserId == Guid.Empty)
            {
                throw new ArgumentException("UserId is required.");
            }

            if (statisticsGame.DurationInSeconds <= 0)
            {
                throw new ArgumentException("Duration must be greater than zero seconds.");
            }

            if (string.IsNullOrWhiteSpace(statisticsGame.Mode))
            {
                throw new ArgumentException("Mode is required.");
            }

            if (statisticsGame.CorrectCharacters < 0 || statisticsGame.IncorrectCharacters < 0 || statisticsGame.ExtraCharacters < 0 || statisticsGame.MissedCharacters < 0)
            {
                throw new ArgumentException("Character counts cannot be negative.");
            }

            var totalCharacters = statisticsGame.CorrectCharacters + statisticsGame.IncorrectCharacters + statisticsGame.ExtraCharacters + statisticsGame.MissedCharacters;
            if (totalCharacters <= 0)
            {
                throw new ArgumentException("At least one character measurement is required.");
            }

            if (!Regex.IsMatch(statisticsGame.Mode, "^[A-Za-z0-9_-]{1,50}$"))
            {
                throw new ArgumentException("Mode must be alphanumeric with optional '-' or '_'.");
            }
        }

        private static DerivedStatistics CalculateDerivedStatistics(StatisticsGameRequestDTO statisticsGame)
        {
            var durationMinutes = statisticsGame.DurationInSeconds / 60m;

            var totalTypedCharacters = statisticsGame.CorrectCharacters + statisticsGame.IncorrectCharacters + statisticsGame.ExtraCharacters;
            var totalMeasuredCharacters = totalTypedCharacters + statisticsGame.MissedCharacters;

            var rawWordsPerMinute = totalTypedCharacters > 0 ? totalTypedCharacters / 5m / durationMinutes : 0m;
            var wordsPerMinute = statisticsGame.CorrectCharacters > 0 ? statisticsGame.CorrectCharacters / 5m / durationMinutes : 0m;
            var accuracy = totalMeasuredCharacters > 0 ? statisticsGame.CorrectCharacters * 100m / totalMeasuredCharacters : 0m;
            var consistency = rawWordsPerMinute > 0 ? wordsPerMinute * 100m / rawWordsPerMinute : 0m;

            return new DerivedStatistics(wordsPerMinute, rawWordsPerMinute, accuracy, consistency);
        }

        private sealed record DerivedStatistics(decimal WordsPerMinute, decimal RawWordsPerMinute, decimal Accuracy, decimal Consistency);

        private static StatisticsGameResponseDTO MapToResponse(StatisticsGame sg)
        {
            return new StatisticsGameResponseDTO
            {
                Id = sg.Id,
                UserId = sg.UserId,
                WordsPerMinute = sg.WordsPerMinute,
                RawWordsPerMinute = sg.RawWordsPerMinute,
                Accuracy = sg.Accuracy,
                Consistency = sg.Consistency,
                CorrectCharacters = sg.CorrectCharacters,
                IncorrectCharacters = sg.IncorrectCharacters,
                ExtraCharacters = sg.ExtraCharacters,
                MissedCharacters = sg.MissedCharacters,
                DurationInSeconds = sg.DurationInSeconds,
                Mode = sg.Mode,
                CreatedAt = sg.CreatedAt
            };
        }

        private async Task UpdateAggregateAsync(Guid userId, DerivedStatistics metrics)
        {
            var aggregate = await _userStatsAggregateRepository.GetByUserIdAsync(userId) ?? new UserStatsAggregate
            {
                UserId = userId,
                GamesCount = 0,
                HighestWordsPerMinute = 0,
                AverageWordsPerMinute = 0,
                HighestRawWordsPerMinute = 0,
                AverageRawWordsPerMinute = 0,
                HighestAccuracy = 0,
                AverageAccuracy = 0,
                HighestConsistency = 0,
                AverageConsistency = 0,
                UpdatedAt = DateTime.UtcNow
            };

            var newCount = aggregate.GamesCount + 1;

            aggregate.HighestWordsPerMinute = Math.Max(aggregate.HighestWordsPerMinute, metrics.WordsPerMinute);
            aggregate.AverageWordsPerMinute = ((aggregate.AverageWordsPerMinute * aggregate.GamesCount) + metrics.WordsPerMinute) / newCount;

            aggregate.HighestRawWordsPerMinute = Math.Max(aggregate.HighestRawWordsPerMinute, metrics.RawWordsPerMinute);
            aggregate.AverageRawWordsPerMinute = ((aggregate.AverageRawWordsPerMinute * aggregate.GamesCount) + metrics.RawWordsPerMinute) / newCount;

            aggregate.HighestAccuracy = Math.Max(aggregate.HighestAccuracy, metrics.Accuracy);
            aggregate.AverageAccuracy = ((aggregate.AverageAccuracy * aggregate.GamesCount) + metrics.Accuracy) / newCount;

            aggregate.HighestConsistency = Math.Max(aggregate.HighestConsistency, metrics.Consistency);
            aggregate.AverageConsistency = ((aggregate.AverageConsistency * aggregate.GamesCount) + metrics.Consistency) / newCount;

            aggregate.GamesCount = newCount;
            aggregate.UpdatedAt = DateTime.UtcNow;

            await _userStatsAggregateRepository.UpsertAsync(aggregate);
        }
    }
}