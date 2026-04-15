using Microsoft.EntityFrameworkCore;
using MonkeyType.Domain.Entities;
using MonkeyType.Domain.IRepositories;
using MonkeyType.Domain.Models;
using MonkeyType.Infrastructure.Context;

namespace MonkeyType.Infrastructure.Repositories
{
    public class StatisticsGameRepository : IStatisticsGameRepository
    {
        private readonly MonkeyTypeDatabaseContext _context;

        public StatisticsGameRepository(MonkeyTypeDatabaseContext context)
        {
            _context = context;
        }
    
        public async Task<IEnumerable<StatisticsGame>?> GetAllAsync()
        {
            return await _context.StatisticsGames
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<PagedResult<StatisticsGame>> GetPagedAsync(int pageNumber, int pageSize)
        {
            var query = _context.StatisticsGames
                .AsNoTracking()
                .OrderByDescending(sg => sg.CreatedAt);

            var totalCount = await query.CountAsync();

            var items = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<StatisticsGame>
            {
                Items = items,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount
            };
        }

        public async Task AddAsync(StatisticsGame statisticsGame)
        {
            await _context.StatisticsGames.AddAsync(statisticsGame);
            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<StatisticsGame>?> GetByUserIdAsync(Guid userId)
        {
            return await _context.StatisticsGames
                .AsNoTracking()
                .Where(sg => sg.UserId == userId)
                .ToListAsync();
        }

        public async Task<PagedResult<StatisticsGame>> GetByUserIdPagedAsync(Guid userId, int pageNumber, int pageSize)
        {
            var query = _context.StatisticsGames
                .AsNoTracking()
                .Where(sg => sg.UserId == userId)
                .OrderByDescending(sg => sg.CreatedAt);

            var totalCount = await query.CountAsync();

            var items = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<StatisticsGame>
            {
                Items = items,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount
            };
        }

        public async Task<StatisticsGame?> GetByIdAsync(Guid id)
        {
            return await _context.StatisticsGames
                .AsNoTracking()
                .FirstOrDefaultAsync(sg => sg.Id == id);
        }

        public async Task<IEnumerable<LeaderboardEntry>> GetLeaderboardAsync(DateTime? startUtc, DateTime? endUtc, int? durationInSeconds, string? mode, int topN)
        {
            var query = _context.StatisticsGames
                .AsNoTracking()
                .Where(sg => sg.DurationInSeconds > 0);

            if (startUtc.HasValue)
            {
                query = query.Where(sg => sg.CreatedAt >= startUtc.Value);
            }

            if (endUtc.HasValue)
            {
                query = query.Where(sg => sg.CreatedAt < endUtc.Value);
            }

            if (durationInSeconds.HasValue)
            {
                query = query.Where(sg => sg.DurationInSeconds == durationInSeconds.Value);
            }

            if (!string.IsNullOrWhiteSpace(mode))
            {
                query = query.Where(sg => sg.Mode == mode);
            }

            var entries = await query
                .Select(sg => new
                {
                    sg.UserId,
                    Username = sg.User.Username,
                    sg.Mode,
                    sg.Accuracy,
                    sg.DurationInSeconds,
                    sg.CreatedAt,
                    sg.WordsPerMinute
                })
                .ToListAsync();

            var leaderboard = entries
                .GroupBy(x => x.UserId)
                .Select(g => g
                    .OrderByDescending(x => x.WordsPerMinute)
                    .ThenByDescending(x => x.Accuracy)
                    .ThenBy(x => x.CreatedAt)
                    .First())
                .OrderByDescending(x => x.WordsPerMinute)
                .ThenByDescending(x => x.Accuracy)
                .ThenBy(x => x.CreatedAt)
                .Take(topN)
                .Select(x => new LeaderboardEntry
                {
                    UserId = x.UserId,
                    Username = x.Username,
                    Mode = x.Mode,
                    Accuracy = x.Accuracy,
                    DurationInSeconds = x.DurationInSeconds,
                    CreatedAt = x.CreatedAt,
                    WordsPerMinute = x.WordsPerMinute
                })
                .ToList();

            return leaderboard;
        }
    }
}