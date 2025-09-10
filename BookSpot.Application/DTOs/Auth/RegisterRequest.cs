using System.ComponentModel.DataAnnotations;

namespace BookSpot.Application.DTOs.Auth;

public class RegisterRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(6)]
    public string Password { get; set; } = string.Empty;

    [Required]
    public string UserType { get; set; } = string.Empty; // "client" or "provider"
}