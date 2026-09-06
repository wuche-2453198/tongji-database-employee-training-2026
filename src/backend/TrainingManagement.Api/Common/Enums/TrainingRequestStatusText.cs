namespace TrainingManagement.Api.Common.Enums;

/// <summary>
/// TRAINING_REQUESTS.STATUS 的数据库取值，与 V001 的 CHECK 约束保持一致。
/// </summary>
public static class TrainingRequestStatusText
{
    public const string Pending = "PENDING";
    public const string DeptApproved = "DEPT_APPROVED";
    public const string DeptRejected = "DEPT_REJECTED";
    public const string HrFiled = "HR_FILED";

    public static bool IsValid(string? status)
    {
        return status is Pending or DeptApproved or DeptRejected or HrFiled;
    }
}
