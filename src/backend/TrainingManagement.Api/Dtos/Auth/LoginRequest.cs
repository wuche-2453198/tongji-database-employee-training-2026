using System.ComponentModel.DataAnnotations;

namespace TrainingManagement.Api.Dtos.Auth;

public sealed class LoginRequest
{
    [Required]
    [MaxLength(100)]
    public string Identifier { get; init; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Password { get; init; } = string.Empty;
}
