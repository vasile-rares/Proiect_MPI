using MonkeyType.Domain.Entities;
using MonkeyType.Domain.IRepositories;
using MonkeyType.Application.IServices;
using MonkeyType.Shared.DTOs.Responses.User;
using MonkeyType.Shared.DTOs.Requests.User;

namespace MonkeyType.Application.Services
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

        public async Task UpdateAsync(UserUpdateRequestDTO existingUser)
        {
            var user = await _userRepository.GetByUsernameAsync(existingUser.Username);
            if (user == null)
            {
                return;
            }

            user.Username = existingUser.Username;
            user.Email = existingUser.Email;
            user.TestsStarted = existingUser.TestsStarted;
            user.TestsCompleted = existingUser.TestsCompleted;
            user.Biography = existingUser.Biography;
            user.UpdatedAt = DateTime.UtcNow;

            await _userRepository.UpdateAsync(user);
        }

        public async Task DeleteAsync(UserResponseDTO user)
        {
            var userEntity = await _userRepository.GetByUsernameAsync(user.Username);
            if (userEntity == null)
            {
                return;
            }

            userEntity.DeletedAt = DateTime.UtcNow;

            await _userRepository.UpdateAsync(userEntity);
        }
    }
}