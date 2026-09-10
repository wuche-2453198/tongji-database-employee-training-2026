using System.ComponentModel.DataAnnotations;

namespace TrainingManagement.Api.Dtos.Auth;

/// <summary>登录请求：账号标识和密码均为必填，长度上限为 100。</summary>
public sealed class LoginRequest
{
    [Required]
    [MaxLength(100)]
    public string Identifier { get; init; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Password { get; init; } = string.Empty;
}
