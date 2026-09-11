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

    /// <summary>有效报名状态：已报名/已签到/已完成均可录入 PRE。</summary>
    private static readonly HashSet<string> ValidRegistrationStatuses =
        new(StringComparer.Ordinal) { "REGISTERED", "SIGNED_IN", "COMPLETED" };

    private readonly ITestRepository _testRepository;

    public TestService(ITestRepository testRepository)
    {
        _testRepository = testRepository;
    }

    public async Task<bool> CreateTestAsync(CreateTestRequest request, int recordedByEmpId)
    {
        var testType = request.TestType?.Trim().ToUpperInvariant() ?? string.Empty;
        if (testType != "PRE" && testType != "POST")
            throw new BusinessException("测试类型只能为PRE或POST");

        if (request.Score < 0 || request.Score > 100)
            throw new BusinessException("测试分数必须在0到100分之间");

        var testedAt = request.TestedAt ?? DateTime.Now;
        if (testedAt > DateTime.Now.AddMinutes(1))
            throw new BusinessException("测试时间不能晚于当前时间");

        var course = await _testRepository.GetCourseGateAsync(request.CourseId);
        if (!course.Exists)
            throw new NotFoundApiException("课程不存在");

        // 报名状态是 PRE/POST 的准入前置：PRE 需有效报名，POST 需已完成培训。
        var registrationStatus = await _testRepository.GetRegistrationStatusAsync(request.EmpId, request.CourseId);
        var now = DateTime.Now;
        if (testType == "PRE")
        {
            if (registrationStatus is null || !ValidRegistrationStatuses.Contains(registrationStatus))
                throw new BusinessException("该员工此课程没有有效报名，不能录入PRE成绩");

            if (course.StartAt.HasValue && now >= course.StartAt.Value)
                throw new BusinessException("课程已开始，不能录入PRE成绩");
        }
        else
        {
            if (!string.Equals(registrationStatus, "COMPLETED", StringComparison.Ordinal))
                throw new BusinessException("该员工尚未完成培训，不能录入POST成绩");
        }

        // 唯一约束 UQ_TESTS_EMP_COURSE_TYPE：同员工+课程+类型只能有一条成绩。
        if (await _testRepository.ExistsByEmployeeCourseAndTypeAsync(request.EmpId, request.CourseId, testType))
            throw new ConflictApiException($"该员工此课程的{TypeLabel(testType)}成绩已存在，不能重复录入");

        request.TestType = testType;
        try
        {
            return await _testRepository.CreateAsync(request, recordedByEmpId, testedAt);
        }
        catch (Exception exception) when (exception.Message.Contains("ORA-00001", StringComparison.Ordinal))
        {
            // 并发写入时由唯一索引兜底，转换为 409。
            throw new ConflictApiException($"该员工此课程的{TypeLabel(testType)}成绩已存在，不能重复录入");
        }
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

    public async Task<PagedResult<TestScoreSummary>> GetScoreSummariesAsync(
        int? employeeId, int? courseId,
        string? employeeName, string? courseName,
        DateTime? startDateFrom, DateTime? startDateTo,
        int page, int pageSize)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize < 1 ? DefaultPageSize : Math.Min(pageSize, MaxPageSize);

        var result = await _testRepository.GetScoreSummariesAsync(
            employeeId, courseId, employeeName, courseName, startDateFrom, startDateTo, page, pageSize);

        foreach (var item in result.Items)
        {
            item.ImprovementRate = ComputeImprovementRate(item.PreScore, item.PostScore);
        }

        return result;
    }

    public async Task<TestImprovementResponse> GetImprovementAsync(int employeeId, int courseId)
    {
        var (pre, post) = await _testRepository.GetScoresAsync(employeeId, courseId);
        return new TestImprovementResponse
        {
            PreScore = pre,
            PostScore = post,
            Improvement = pre.HasValue && post.HasValue ? post.Value - pre.Value : null,
            ImprovementRate = ComputeImprovementRate(pre, post)
        };
    }

    // 提升率 = (POST - PRE) / PRE * 100；PRE 缺失或为 0 时无法计算，返回 null（不视为 0 分无效）。
    private static decimal? ComputeImprovementRate(decimal? pre, decimal? post)
    {
        if (!pre.HasValue || !post.HasValue || pre.Value == 0m)
        {
            return null;
        }

        return Math.Round((post.Value - pre.Value) / pre.Value * 100m, 1, MidpointRounding.AwayFromZero);
    }

    private static string TypeLabel(string testType) => testType == "PRE" ? "训前" : "训后";
}
