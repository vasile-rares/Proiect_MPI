using Microsoft.EntityFrameworkCore;
using MonkeyType.Domain.Entities;
using MonkeyType.Domain.IRepositories;
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
            return await _context.StatisticsGames.ToListAsync();
        }

        public async Task AddAsync(StatisticsGame statisticsGame)
        {
            await _context.StatisticsGames.AddAsync(statisticsGame);
            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<StatisticsGame>?> GetByUserIdAsync(Guid userId)
        {
            return await _context.StatisticsGames.Where(sg => sg.UserId == userId).ToListAsync();
        }

        public async Task<StatisticsGame?> GetByIdAsync(Guid id)
        {
            return await _context.StatisticsGames.FindAsync(id);
        }
    }
}