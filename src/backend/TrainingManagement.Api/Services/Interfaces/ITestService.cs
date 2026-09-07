using System.Collections.Generic;
using System.Threading.Tasks;
using TrainingManagement.Api.Dtos.Tests;
using TrainingManagement.Api.Entities;

namespace TrainingManagement.Api.Services.Interfaces;
public interface ITestService
{
    Task<bool> CreateTestAsync(CreateTestRequest request, int recordedByEmpId);
    Task<IEnumerable<TrainingTest>> GetTestListAsync(int? employeeId, int? courseId, string? testType);
}
