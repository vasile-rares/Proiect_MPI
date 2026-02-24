namespace MonkeyType.Shared.DTOs.Responses.User
{
    public class UserResponseDTO
    {
        public string Username { get; set; }
        public string Email { get; set; }
        public int TestsStarted { get; set; }
        public int TestsCompleted { get; set; }
        public string Biography { get; set; }
    }
}