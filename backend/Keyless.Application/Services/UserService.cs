using Keyless.Domain.Entities;
using Keyless.Domain.IRepositories;
using Keyless.Application.IServices;
using Keyless.Shared.DTOs.Responses.User;
using Keyless.Shared.DTOs.Requests.User;

namespace Keyless.Application.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;

        public UserService(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<UserResponseDTO?> GetByIdAsync(Guid id)
        {
            var user = await _userRepository.GetByIdAsync(id);
            if (user == null)
            {
                return null;
            }

            return new UserResponseDTO
            {
                Username = user.Username,
                Email = user.Email,
                TestsStarted = user.TestsStarted,
                TestsCompleted = user.TestsCompleted,
                Biography = user.Biography
            };
        }

        public async Task<User?> GetByUsernameAsync(string username)
        {
            return await _userRepository.GetByUsernameAsync(username);
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            return await _userRepository.GetByEmailAsync(email);
        }

        public async Task AddAsync(User user)
        {
            await _userRepository.AddAsync(user);
        }

        public async Task<bool> UpdateAsync(Guid id, UserUpdateRequestDTO existingUser)
        {
            var user = await _userRepository.GetByIdAsync(id);
            if (user == null)
            {
                return false;
            }

            user.Username = existingUser.Username;
            user.Email = existingUser.Email;
            user.TestsStarted = existingUser.TestsStarted;
            user.TestsCompleted = existingUser.TestsCompleted;
            user.Biography = existingUser.Biography;
            user.UpdatedAt = DateTime.UtcNow;

            await _userRepository.UpdateAsync(user);
            return true;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var userEntity = await _userRepository.GetByIdAsync(id);
            if (userEntity == null)
            {
                return false;
            }

            userEntity.DeletedAt = DateTime.UtcNow;

            await _userRepository.UpdateAsync(userEntity);
            return true;
        }
    }
}