using Keyless.Domain.Entities;
using Keyless.Shared.DTOs.Responses.User;
using Keyless.Shared.DTOs.Requests.User;

namespace Keyless.Application.IServices
{
    public interface IUserService
    {
        Task<UserResponseDTO?> GetByIdAsync(Guid id);
        Task<User?> GetByUsernameAsync(string username);
        Task<User?> GetByEmailAsync(string email);
        Task AddAsync(User user);
        Task<bool> UpdateAsync(Guid id, UserUpdateRequestDTO user);
        Task<bool> DeleteAsync(Guid id);
    }
}