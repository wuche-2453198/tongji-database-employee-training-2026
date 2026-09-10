using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TrainingManagement.Api.Common.Exceptions;
using TrainingManagement.Api.Common.Responses;
using TrainingManagement.Api.Dtos.Tests;
using TrainingManagement.Api.Entities;
using TrainingManagement.Api.Repositories.Interfaces;
using TrainingManagement.Api.Services.Interfaces;

namespace TrainingManagement.Api.Services.Implementations;
public sealed class TestService : ITestService
{
    private const int MaxPageSize = 100;
    private const int DefaultPageSize = 20;

    private readonly ITestRepository _testRepository;

    public TestService(ITestRepository testRepository)
    {
        _testRepository = testRepository;
    }

    public async Task<bool> CreateTestAsync(CreateTestRequest request, int recordedByEmpId)
    {
        if (request.Score < 0 || request.Score > 100)
            throw new BusinessException("测试分数必须在0到100分之间");

        if (request.TestType != "PRE" && request.TestType != "POST")
            throw new BusinessException("测试类型只能为PRE或POST");

        // D-012:PRE 在 HR 备案后、课程开始前录入;POST 在课程结束后录入。
        var course = await _testRepository.GetCourseGateAsync(request.CourseId);
        if (!course.Exists)
            throw new NotFoundApiException("课程不存在");

        var now = DateTime.Now;
        if (request.TestType == "PRE")
        {
            if (!await _testRepository.HasHrFiledRequestAsync(request.EmpId, request.CourseId))
                throw new BusinessException("该员工此课程的申请尚未完成HR备案，不能录入PRE成绩");

            if (course.StartAt.HasValue && now >= course.StartAt.Value)
                throw new BusinessException("课程已开始，不能录入PRE成绩");
        }
        else
        {
            if (course.EndAt.HasValue && now < course.EndAt.Value)
                throw new BusinessException("课程尚未结束，不能录入POST成绩");
        }

        var exists = await _testRepository.ExistsByEmployeeCourseAndTypeAsync(request.EmpId, request.CourseId, request.TestType);
        if (exists)
            return await _testRepository.UpdateScoreAsync(request, recordedByEmpId);

        return await _testRepository.CreateAsync(request, recordedByEmpId);
    }

    public Task<PagedResult<TrainingTest>> GetTestListAsync(
        int? employeeId, int? courseId, string? testType,
        string? employeeName, string? courseName,
        DateTime? startDateFrom, DateTime? startDateTo,
        int page, int pageSize)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize < 1 ? DefaultPageSize : Math.Min(pageSize, MaxPageSize);
        return _testRepository.GetPagedListAsync(
            employeeId, courseId, testType, employeeName, courseName, startDateFrom, startDateTo, page, pageSize);
    }

    public async Task<TestImprovementResponse> GetImprovementAsync(int employeeId, int courseId)
    {
        var (pre, post) = await _testRepository.GetScoresAsync(employeeId, courseId);
        return new TestImprovementResponse
        {
            PreScore = pre,
            PostScore = post,
            Improvement = pre.HasValue && post.HasValue ? post.Value - pre.Value : null,
        };
    }
}
