namespace TrainingManagement.Api.Dtos.Organization;

// 黑名单响应
public sealed class BlacklistResponse
{
    // 黑名单记录编号
    public long BlackId { get; set; }

    // 员工编号
    public long EmpId { get; set; }

    // 黑名单原因
    public string Reason { get; set; } = string.Empty;

    // 开始日期
    public DateTime StartDate { get; set; }

    // 结束日期
    public DateTime? EndDate { get; set; }

    // 黑名单状态：ACTIVE/RELEASED
    public string Status { get; set; } = string.Empty;
}