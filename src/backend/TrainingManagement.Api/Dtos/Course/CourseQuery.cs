using System.ComponentModel.DataAnnotations;

namespace TrainingManagement.Api.Dtos.Course;

public sealed class CourseQuery
{
    [MaxLength(100)]
    public string? CourseName { get; set; }

    [RegularExpression(
        "^(技术培训|管理培训|产品培训|营销培训)$",
        ErrorMessage = "课程类型只能是技术培训、管理培训、产品培训、营销培训")]
    public string? CourseType { get; set; }

    [RegularExpression(
        "^(DRAFT|PUBLISHED|CLOSED)$",
        ErrorMessage = "课程状态只能是DRAFT、PUBLISHED或CLOSED")]
    public string? CourseStatus { get; set; }

    public DateTime? StartAtFrom { get; set; }

    public DateTime? StartAtTo { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "页码必须大于0")]
    public int Page { get; set; } = 1;

    [Range(1, 100, ErrorMessage = "每页数量必须在1到100之间")]
    public int PageSize { get; set; } = 20;
}
