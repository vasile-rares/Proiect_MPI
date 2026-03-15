namespace MonkeyType.Shared.DTOs.Requests.User
{
    public class RegisterRequestDTO
    {
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string VerifyEmail { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string VerifyPassword { get; set; } = string.Empty;
    }
}