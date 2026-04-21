using System.ComponentModel.DataAnnotations;

namespace Keyless.Shared.DTOs.Requests.User
{
    public class RegisterRequestDTO
    {
        [Required]
        [MinLength(3)]
        [MaxLength(50)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [Compare("Email", ErrorMessage = "Emails do not match.")]
        public string VerifyEmail { get; set; } = string.Empty;

        [Required]
        [MinLength(8)]
        [MaxLength(128)]
        public string Password { get; set; } = string.Empty;

        [Required]
        [Compare("Password", ErrorMessage = "Passwords do not match.")]
        [MaxLength(128)]
        public string VerifyPassword { get; set; } = string.Empty;
    }
}