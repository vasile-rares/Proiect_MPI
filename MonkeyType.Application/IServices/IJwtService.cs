namespace MonkeyType.Application.IServices
{
    public interface IJwtService
    {
        string GenerateToken(Guid userId, string username);
        Guid? ValidateToken(string token);
    }
}