using System.Collections.Generic;
using System.Threading.Tasks;
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
}
