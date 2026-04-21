using Keyless.Domain.Entities;

namespace Keyless.Domain.IRepositories
{
    public interface IUserStatsAggregateRepository
    {
        Task<UserStatsAggregate?> GetByUserIdAsync(Guid userId);
        Task UpsertAsync(UserStatsAggregate aggregate);
    }
}
