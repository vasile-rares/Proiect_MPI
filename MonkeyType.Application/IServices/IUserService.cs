using MonkeyType.Domain.Entities;

namespace MonkeyType.Application.IServices
{
    public interface IUserService
    {
        Task <User?> GetByIdAsync(Guid id);
        Task <User?> GetByUsernameAsync(string username);
        Task AddAsync(User user);
        Task UpdateAsync(User user);
        Task DeleteAsync(User user);
    }
}