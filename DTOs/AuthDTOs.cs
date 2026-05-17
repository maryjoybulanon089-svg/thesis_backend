using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace ThesisRepository.DTOs
{
    public class LoginDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;
    }

    public class SignUpDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MinLength(8)]
        public string Password { get; set; } = string.Empty;

        public string FirstName { get; set; } = string.Empty;

        public string LastName { get; set; } = string.Empty;

        public string? Name { get; set; }

        [JsonPropertyName("fullName")]
        public string? FullName { get; set; }

        [Required]
        public string Role { get; set; } = string.Empty;

        public string? Department { get; set; }
    }

    public class AuthResponseDto
    {
        public string Token { get; set; } = string.Empty;
        public UserDto User { get; set; } = null!;
    }

    /// <summary>
    /// Represents a User returned to the frontend.
    /// int UserId is exposed as a string "Id" for frontend compatibility.
    /// </summary>
    public class UserDto
    {
        public string Id { get; set; } = string.Empty;         // UserId.ToString()
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public bool IsApproved { get; set; }
        public bool IsActive { get; set; }
        public string? Department { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLogin { get; set; }
    }
}
