using System.Threading.Tasks;

namespace TrainingManagement.Api.Repositories.Interfaces
{
    public interface ICertificateRepository
    {
        Task<object> GetRegistrationByIdAsync(int registrationId);
        Task<bool> ExistsByEmployeeAndCourseAsync(int employeeId, int courseId);
        Task<int> CreateAsync(string certificateNo, int employeeId, int courseId, int registrationId);
        Task<object> GetByEmployeeIdAsync(int employeeId);
        Task<object> GetByIdAsync(int id);
        Task<bool> UpdateNotifyFlagAsync(int id);
    }
}
