using System.Collections.Generic;
using System.Threading.Tasks;
using TrainingManagement.Api.Dtos.Tests;
using TrainingManagement.Api.Entities;

namespace TrainingManagement.Api.Repositories.Interfaces;
public interface ITestRepository
{
    Task<bool> CreateAsync(CreateTestRequest request, int recordedByEmpId);
    Task<bool> UpdateScoreAsync(CreateTestRequest request, int recordedByEmpId);
    Task<bool> ExistsByEmployeeCourseAndTypeAsync(int employeeId, int courseId, string testType);
    Task<IEnumerable<TrainingTest>> GetListAsync(int? employeeId, int? courseId, string? testType);
}
