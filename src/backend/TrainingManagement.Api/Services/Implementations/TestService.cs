using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TrainingManagement.Api.Common.Exceptions;
using TrainingManagement.Api.Dtos.Tests;
using TrainingManagement.Api.Entities;
using TrainingManagement.Api.Repositories.Interfaces;
using TrainingManagement.Api.Services.Interfaces;

namespace TrainingManagement.Api.Services.Implementations;
public sealed class TestService : ITestService
{
    private readonly ITestRepository _testRepository;

    public TestService(ITestRepository testRepository)
    {
        _testRepository = testRepository;
    }

    public async Task<bool> CreateTestAsync(CreateTestRequest request, int recordedByEmpId)
    {
        if (request.Score < 0 || request.Score > 100)
            throw new BadRequestException("测试分数必须在0到100分之间");

        if (request.TestType != "PRE" && request.TestType != "POST")
            throw new BadRequestException("测试类型只能为PRE或POST");

        var exists = await _testRepository.ExistsByEmployeeCourseAndTypeAsync(request.EmpId, request.CourseId, request.TestType);
        if (exists)
            return await _testRepository.UpdateScoreAsync(request, recordedByEmpId);
        else
            return await _testRepository.CreateAsync(request, recordedByEmpId);
    }

    public async Task<IEnumerable<TrainingTest>> GetTestListAsync(int? employeeId, int? courseId, string? testType)
    {
        return await _testRepository.GetListAsync(employeeId, courseId, testType);
    }
}
