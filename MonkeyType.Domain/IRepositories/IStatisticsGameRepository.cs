using MonkeyType.Domain.Entities;

namespace MonkeyType.Domain.IRepositories
{
    public interface IStatisticsGameRepository
    {
        Task<IEnumerable<StatisticsGame>?> GetAllAsync();
        Task AddAsync(StatisticsGame statisticsGame);
        Task<IEnumerable<StatisticsGame>?> GetByUserIdAsync(Guid userId);
        Task<StatisticsGame?> GetByIdAsync(Guid id);
    }
}