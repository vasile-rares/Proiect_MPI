using MonkeyType.Domain.Entities;
using MonkeyType.Domain.IRepositories;
using MonkeyType.Application.IServices;

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

        public async Task AddAsync(StatisticsGame statisticsGame)
        {
            await _statisticsGameRepository.AddAsync(statisticsGame);
        }

        public async Task<IEnumerable<StatisticsGame>?> GetByUserIdAsync(Guid userId)
        {
            return await _statisticsGameRepository.GetByUserIdAsync(userId);
        }

        public async Task<StatisticsGame?> GetByIdAsync(Guid id)
        {
            return await _statisticsGameRepository.GetByIdAsync(id);
        }
    }
}