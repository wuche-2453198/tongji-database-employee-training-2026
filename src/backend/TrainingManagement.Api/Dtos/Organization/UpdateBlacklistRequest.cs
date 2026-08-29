namespace TrainingManagement.Api.Dtos.Organization;

// 更新黑名单请求
public sealed class UpdateBlacklistRequest
{
    // 黑名单原因
    public string? Reason { get; set; }

    // 结束日期
    public DateTime? EndDate { get; set; }

    // 黑名单状态：ACTIVE/RELEASED
    public string? Status { get; set; }
}