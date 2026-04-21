namespace Keyless.Shared.DTOs.Responses.User
{
    public class UserResponseDTO
    {
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int TestsStarted { get; set; }
        public int TestsCompleted { get; set; }
        public string? Biography { get; set; } = string.Empty;
    }
}