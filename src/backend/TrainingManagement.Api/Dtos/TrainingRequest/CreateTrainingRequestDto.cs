using System.ComponentModel.DataAnnotations;

namespace TrainingManagement.Api.Dtos.TrainingRequest;

public class CreateTrainingRequestDto
{
    [Range(1, int.MaxValue, ErrorMessage = "课程ID必须大于0")]
    public int CourseId { get; set; }

    [Required(ErrorMessage = "申请理由不能为空")]
    [StringLength(500, ErrorMessage = "申请理由不能超过500个字符")]
    public string RequestReason { get; set; } = string.Empty;
}
