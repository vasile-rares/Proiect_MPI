using Keyless.Application.IServices;

namespace Keyless.Application.Services
{
    public class AuthenticationService : IAuthenticationService
    {
        public Task<string> HashPasswordAsync(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
            {
                throw new ArgumentException("Password cannot be null or empty.", nameof(password));
            }

            var passwordHash = BCrypt.Net.BCrypt.HashPassword(password);
            return Task.FromResult(passwordHash);
        }

        public Task<bool> VerifyPasswordAsync(string password, string passwordHash)
        {
            if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(passwordHash))
            {
                return Task.FromResult(false);
            }

            var isValid = BCrypt.Net.BCrypt.Verify(password, passwordHash);
            return Task.FromResult(isValid);
        }
    }
}