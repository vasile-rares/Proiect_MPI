using Microsoft.EntityFrameworkCore;
using MonkeyType.Domain.Entities;
using MonkeyType.Domain.IRepositories;
using MonkeyType.Infrastructure.Context;

namespace MonkeyType.Infrastructure.Repositories
{
    public class UserStatsAggregateRepository : IUserStatsAggregateRepository
    {
        private readonly MonkeyTypeDatabaseContext _context;

        public UserStatsAggregateRepository(MonkeyTypeDatabaseContext context)
        {
            _context = context;
        }

        public async Task<UserStatsAggregate?> GetByUserIdAsync(Guid userId)
        {
            return await _context.UserStatsAggregates
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.UserId == userId);
        }

        public async Task UpsertAsync(UserStatsAggregate aggregate)
        {
            if (_context.Database.IsSqlite())
            {
                await _context.Database.ExecuteSqlInterpolatedAsync($@"
                    INSERT INTO ""UserStatsAggregates"" (
                        ""UserId"",
                        ""GamesCount"",
                        ""HighestWordsPerMinute"",
                        ""AverageWordsPerMinute"",
                        ""HighestRawWordsPerMinute"",
                        ""AverageRawWordsPerMinute"",
                        ""HighestAccuracy"",
                        ""AverageAccuracy"",
                        ""HighestConsistency"",
                        ""AverageConsistency"",
                        ""UpdatedAt""
                    )
                    VALUES (
                        {aggregate.UserId},
                        {aggregate.GamesCount},
                        {aggregate.HighestWordsPerMinute},
                        {aggregate.AverageWordsPerMinute},
                        {aggregate.HighestRawWordsPerMinute},
                        {aggregate.AverageRawWordsPerMinute},
                        {aggregate.HighestAccuracy},
                        {aggregate.AverageAccuracy},
                        {aggregate.HighestConsistency},
                        {aggregate.AverageConsistency},
                        {aggregate.UpdatedAt}
                    )
                    ON CONFLICT(""UserId"") DO UPDATE SET
                        ""GamesCount"" = excluded.""GamesCount"",
                        ""HighestWordsPerMinute"" = excluded.""HighestWordsPerMinute"",
                        ""AverageWordsPerMinute"" = excluded.""AverageWordsPerMinute"",
                        ""HighestRawWordsPerMinute"" = excluded.""HighestRawWordsPerMinute"",
                        ""AverageRawWordsPerMinute"" = excluded.""AverageRawWordsPerMinute"",
                        ""HighestAccuracy"" = excluded.""HighestAccuracy"",
                        ""AverageAccuracy"" = excluded.""AverageAccuracy"",
                        ""HighestConsistency"" = excluded.""HighestConsistency"",
                        ""AverageConsistency"" = excluded.""AverageConsistency"",
                        ""UpdatedAt"" = excluded.""UpdatedAt"";");

                return;
            }

            var existing = await _context.UserStatsAggregates.FirstOrDefaultAsync(x => x.UserId == aggregate.UserId);
            if (existing == null)
            {
                await _context.UserStatsAggregates.AddAsync(aggregate);
            }
            else
            {
                existing.GamesCount = aggregate.GamesCount;
                existing.HighestWordsPerMinute = aggregate.HighestWordsPerMinute;
                existing.AverageWordsPerMinute = aggregate.AverageWordsPerMinute;
                existing.HighestRawWordsPerMinute = aggregate.HighestRawWordsPerMinute;
                existing.AverageRawWordsPerMinute = aggregate.AverageRawWordsPerMinute;
                existing.HighestAccuracy = aggregate.HighestAccuracy;
                existing.AverageAccuracy = aggregate.AverageAccuracy;
                existing.HighestConsistency = aggregate.HighestConsistency;
                existing.AverageConsistency = aggregate.AverageConsistency;
                existing.UpdatedAt = aggregate.UpdatedAt;
            }

            await _context.SaveChangesAsync();
        }
    }
}
