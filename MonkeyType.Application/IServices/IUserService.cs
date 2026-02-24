using MonkeyType.Domain.Entities;
using MonkeyType.Shared.DTOs.Responses.User;

namespace MonkeyType.Application.IServices
{
    public interface IUserService
    {
        Task <UserResponseDTO?> GetByIdAsync(Guid id);
        Task <User?> GetByUsernameAsync(string username);
        Task<User?> GetByEmailAsync(string email);
        Task AddAsync(User user);
        Task UpdateAsync(User user);
        Task DeleteAsync(User user);
    }
}