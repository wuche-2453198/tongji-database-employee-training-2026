using System.ComponentModel.DataAnnotations;

namespace TrainingManagement.Api.Dtos.Registration;

public sealed class RegistrationQuery
{
    [Range(1, long.MaxValue, ErrorMessage = "课程ID必须大于0")]
    public long? CourseId { get; set; }

    [Range(1, long.MaxValue, ErrorMessage = "员工ID必须大于0")]
    public long? EmpId { get; set; }

    [Range(1, long.MaxValue, ErrorMessage = "部门ID必须大于0")]
    public long? DeptId { get; set; }

    [MaxLength(50, ErrorMessage = "员工姓名不能超过50个字符")]
    public string? EmpName { get; set; }

    [MaxLength(200, ErrorMessage = "课程名称不能超过200个字符")]
    public string? CourseName { get; set; }

    [RegularExpression(
        "^(REGISTERED|SIGNED_IN|ABSENT|COMPLETED|CANCELED)$",
        ErrorMessage = "报名状态只能是REGISTERED、SIGNED_IN、ABSENT、COMPLETED或CANCELED")]
    public string? Status { get; set; }

    public bool? SignedIn { get; set; }

    public DateTime? StartAtFrom { get; set; }

    public DateTime? StartAtTo { get; set; }

    [RegularExpression(
        "^(startTime|regDate|completedAt)$",
        ErrorMessage = "排序字段只能是startTime、regDate或completedAt")]
    public string? SortBy { get; set; } = "startTime";

    [RegularExpression(
        "^(asc|desc)$",
        ErrorMessage = "排序方向只能是asc或desc")]
    public string? SortDirection { get; set; } = "desc";

    [Range(1, int.MaxValue, ErrorMessage = "页码必须大于0")]
    public int Page { get; set; } = 1;

    [Range(1, 100, ErrorMessage = "每页数量必须在1到100之间")]
    public int PageSize { get; set; } = 20;
}
