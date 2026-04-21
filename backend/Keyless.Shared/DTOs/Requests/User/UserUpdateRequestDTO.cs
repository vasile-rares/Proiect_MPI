using System.ComponentModel.DataAnnotations;

namespace Keyless.Shared.DTOs.Requests.User
{
    public class UserUpdateRequestDTO
    {
        [Required]
        [MinLength(3)]
        [MaxLength(50)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Range(0, int.MaxValue)]
        public int TestsStarted { get; set; }

        [Range(0, int.MaxValue)]
        public int TestsCompleted { get; set; }

        public string? Biography { get; set; }
    }
}