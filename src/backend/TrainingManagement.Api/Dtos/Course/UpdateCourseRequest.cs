using System.ComponentModel.DataAnnotations;

namespace TrainingManagement.Api.Dtos.Course;

public sealed class UpdateCourseRequest
{
    [Required]
    [MaxLength(100)]
    public string CourseName { get; set; } = string.Empty;

    [Required]
    [RegularExpression("^(技术培训|管理培训|产品培训|营销培训)$",
        ErrorMessage = "课程类型只能是技术培训、管理培训、产品培训、营销培训")]
    public string CourseType { get; set; } = string.Empty;

    [Range(typeof(decimal), "0.1", "9999", ErrorMessage = "学时必须大于0")]
    public decimal DurationHours { get; set; }

    [Range(1, long.MaxValue, ErrorMessage = "必须选择讲师")]
    public long TrainerId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "最大人数必须大于0")]
    public int MaxStudents { get; set; }

    public DateTime StartAt { get; set; }

    public DateTime EndAt { get; set; }

    [MaxLength(100)]
    public string Location { get; set; } = string.Empty;

    [Range(typeof(decimal), "0", "999999999", ErrorMessage = "预算金额不能为负数")]
    public decimal BudgetAmount { get; set; }

    [Range(1, long.MaxValue, ErrorMessage = "必须选择主办部门")]
    public long DeptId { get; set; }

    [MaxLength(500)]
    public string? PreTestUrl { get; set; }

    [MaxLength(500)]
    public string? PostTestUrl { get; set; }

    [MaxLength(500)]
    public string? MaterialUrl { get; set; }
}