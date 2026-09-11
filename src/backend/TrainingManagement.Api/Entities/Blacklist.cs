namespace TrainingManagement.Api.Entities;

// 黑名单实体，对应数据库BLACKLIST表
public sealed class Blacklist
{
    // 黑名单记录编号（主键，序列生成）
    public long BlackId { get; set; }

    // 员工编号（外键，关联EMPLOYEES）
    public long EmpId { get; set; }

    // 黑名单原因
    public string Reason { get; set; } = string.Empty;

    // 开始日期
    public DateTime StartDate { get; set; }

    // 结束日期
    public DateTime? EndDate { get; set; }

    // 黑名单状态：ACTIVE/RELEASED
    public string Status { get; set; } = "ACTIVE";

    // 操作人员工编号（审计字段，历史数据可为空）
    public long? OperatorEmpId { get; set; }

    // 创建时间
    public DateTime CreatedAt { get; set; }

    // 员工姓名（联表展示字段，非持久化）
    public string EmpName { get; set; } = string.Empty;

    // 员工部门名称（联表展示字段，非持久化）
    public string DeptName { get; set; } = string.Empty;
}