namespace TrainingManagement.Api.Dtos.Organization;

// 新增黑名单请求
public sealed class CreateBlacklistRequest
{
    // 员工编号（必填）
    public long EmpId { get; set; }

    // 黑名单原因（必填）
    public string Reason { get; set; } = string.Empty;

    // 开始日期
    public DateTime? StartDate { get; set; }

    // 结束日期
    public DateTime? EndDate { get; set; }
}