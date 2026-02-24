namespace MonkeyType.Shared.DTOs.Requests.User
{
    public class RegisterRequestDTO
    {
        public string Username { get; set; }
        public string Email { get; set; }
        public string VerifyEmail { get; set; }
        public string Password { get; set; }
        public string VerifyPassword { get; set; }
    }
}