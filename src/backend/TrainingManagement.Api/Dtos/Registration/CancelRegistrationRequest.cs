using System.ComponentModel.DataAnnotations;

namespace TrainingManagement.Api.Dtos.Registration;

public sealed class CancelRegistrationRequest
{
    [MaxLength(500, ErrorMessage = "取消原因不能超过500个字符")]
    public string? Reason { get; set; }
}
