using System;
using System.Threading.Tasks;
using TrainingManagement.Api.Dtos.Tests;
using TrainingManagement.Api.Repositories.Interfaces;
using TrainingManagement.Api.Services.Interfaces;

namespace TrainingManagement.Api.Services.Implementations
{
    public class TestService : ITestService
    {
        private readonly ITestRepository _testRepository;

        public TestService(ITestRepository testRepository)
        {
            _testRepository = testRepository;
        }

        public async Task<bool> CreateTestAsync(CreateTestRequest request)
        {
            if (request.Score < 0 || request.Score > 100)
            {
                throw new ArgumentException("测试分数必须在0到100分之间");
            }

            if (request.TestType != "PRE" && request.TestType != "POST")
            {
                throw new ArgumentException("测试类型只能为PRE或POST");
            }

            var exists = await _testRepository.ExistsByEmployeeCourseAndTypeAsync(
                request.EmployeeId, request.CourseId, request.TestType);

            if (exists)
            {
                return await _testRepository.UpdateScoreAsync(request);
            }
            else
            {
                return await _testRepository.CreateAsync(request);
            }
        }

        public async Task<object> GetTestListAsync(int? employeeId, int? courseId, string? testType)
        {
            return await _testRepository.GetListAsync(employeeId, courseId, testType);
        }
    }
}
