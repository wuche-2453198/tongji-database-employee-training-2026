namespace TrainingManagement.Api.Entities;

/// <summary>
/// 提交申请前的员工门禁数据。组织模块 Service 就绪后由其公开契约替代。
/// 注意：保持普通 get/set 属性，Dapper 对 record/init-only 的物化在本项目 ODP.NET 环境不可靠。
/// </summary>
public sealed class EmployeeGate
{
    public bool Exists { get; set; }

    public string? Status { get; set; }
}

/// <summary>
/// 提交申请前的课程门禁数据。课程模块 Service 就绪后由其公开契约替代。
/// </summary>
public sealed class CourseGate
{
    public bool Exists { get; set; }

    public string? Status { get; set; }

    public DateTime? StartAt { get; set; }
}
