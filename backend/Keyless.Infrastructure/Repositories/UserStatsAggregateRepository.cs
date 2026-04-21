using Microsoft.EntityFrameworkCore;
using Keyless.Domain.Entities;
using Keyless.Domain.IRepositories;
using Keyless.Infrastructure.Context;

namespace Keyless.Infrastructure.Repositories
{
    public class UserStatsAggregateRepository : IUserStatsAggregateRepository
    {
        private readonly KeylessDatabaseContext _context;

        public UserStatsAggregateRepository(KeylessDatabaseContext context)
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
