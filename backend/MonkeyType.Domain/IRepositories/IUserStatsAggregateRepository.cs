using MonkeyType.Domain.Entities;

namespace MonkeyType.Domain.IRepositories
{
    public interface IUserStatsAggregateRepository
    {
        Task<UserStatsAggregate?> GetByUserIdAsync(Guid userId);
        Task UpsertAsync(UserStatsAggregate aggregate);
    }
}
