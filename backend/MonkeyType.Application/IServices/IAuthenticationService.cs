namespace MonkeyType.Application.IServices
{
    public interface IAuthenticationService
    {
        Task<string> HashPasswordAsync(string password);
        Task<bool> VerifyPasswordAsync(string password, string passwordHash);
    }
}