namespace TrainingManagement.Api.Dtos.TrainingRequest;

public class TrainingRequestQueryDto
{
    public string? Status { get; set; }

    public int? EmployeeId { get; set; }

    public int? CourseId { get; set; }

    /// <summary>按所属部门名称模糊筛选（主管审批页与 HR 备案页使用）。</summary>
    public string? DepartmentName { get; set; }

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 20;
}
