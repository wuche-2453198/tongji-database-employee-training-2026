using System.ComponentModel.DataAnnotations;

namespace TrainingManagement.Api.Dtos.Attendance;

public sealed class CreateManualAttendanceRequest
{
    [Range(1, long.MaxValue, ErrorMessage = "必须选择报名记录")]
    public long RegId { get; set; }

    public DateTime? SigninTime { get; set; }

    [Required(ErrorMessage = "补签必须填写备注")]
    [MaxLength(500, ErrorMessage = "备注不能超过500个字符")]
    public string Remark { get; set; } = string.Empty;
}
