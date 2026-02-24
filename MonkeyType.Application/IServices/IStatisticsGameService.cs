using MonkeyType.Domain.Entities;

namespace MonkeyType.Application.IServices
{
    public interface IStatisticsGameService
    {
        Task<IEnumerable<StatisticsGame>?> GetAllAsync();
        Task AddAsync(StatisticsGame statisticsGame);
        Task<IEnumerable<StatisticsGame>?> GetByUserIdAsync(Guid userId);
        Task<StatisticsGame?> GetByIdAsync(Guid id);
    }
}