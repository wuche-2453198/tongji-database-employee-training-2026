using TrainingManagement.Api.Entities;

namespace TrainingManagement.Api.Repositories.Interfaces;

public interface ICertificateRepository
{
    Task<Registration?> GetRegistrationByIdAsync(int registrationId);

    Task<bool> ExistsByEmployeeAndCourseAsync(int employeeId, int courseId);

    Task<int> CreateAsync(string certificateNo, int employeeId, int courseId, int issuedByEmpId);

    Task<IEnumerable<TrainingCertificate>> GetByEmployeeIdAsync(int employeeId);

    Task<TrainingCertificate?> GetByIdAsync(int id);

    Task<bool> UpdateNotifyFlagAsync(int id);

    /// <summary>课程门禁:训后测试配置与起止时间,用于发证校验。</summary>
    Task<ResultCourseGate> GetCourseGateAsync(int courseId);

    /// <summary>员工在某课程的 POST 成绩,未录入返回 null。</summary>
    Task<decimal?> GetPostTestScoreAsync(int employeeId, int courseId);
}
