using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TrainingManagement.Api.Common.Exceptions;
using TrainingManagement.Api.Common.Extensions;
using TrainingManagement.Api.Common.Responses;
using TrainingManagement.Api.Common.Security;
using TrainingManagement.Api.Dtos.Tests;
using TrainingManagement.Api.Dtos.TrainingRequest;
using TrainingManagement.Api.Entities;
using TrainingManagement.Api.Services.Interfaces;

namespace TrainingManagement.Api.Controllers;

[Authorize]
[Route("api/tests")]
public sealed class TestsController : ApiControllerBase
{
    private readonly ITestService _testService;

    public TestsController(ITestService testService)
    {
        _testService = testService;
    }

    [Authorize(Roles = RoleCodes.Hr + "," + RoleCodes.Admin)]
    [HttpPost]
    public async Task<ActionResult<ApiResponse<bool>>> Create([FromBody] CreateTestRequest request)
    {
        var recorderId = GetCurrentEmployeeId();
        var result = await _testService.CreateTestAsync(request, recorderId);
        return OkResponse(result, "成绩录入成功");
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<TrainingTest>>>> GetList(
        [FromQuery] int? employeeId,
        [FromQuery] int? courseId,
        [FromQuery] string? testType,
        [FromQuery] string? employeeName,
        [FromQuery] string? courseName,
        [FromQuery] DateTime? startDateFrom,
        [FromQuery] DateTime? startDateTo,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var actor = GetActor();

        // 越权防护:普通员工只能查看本人成绩;HR/管理员可查任意员工。
        var currentEmployeeId = GetCurrentEmployeeId();
        if (!actor.IsAdmin && !actor.IsHr)
        {
            employeeId = currentEmployeeId;
        }

        var result = await _testService.GetTestListAsync(
            employeeId, courseId, testType, employeeName, courseName, startDateFrom, startDateTo, page, pageSize);
        return OkResponse(result, "查询成功");
    }

    /// <summary>按员工+课程聚合的成绩汇总：PRE/POST/分数变化/提升率。</summary>
    [HttpGet("summary")]
    public async Task<ActionResult<ApiResponse<PagedResult<TestScoreSummary>>>> GetSummary(
        [FromQuery] int? employeeId,
        [FromQuery] int? courseId,
        [FromQuery] string? employeeName,
        [FromQuery] string? courseName,
        [FromQuery] DateTime? startDateFrom,
        [FromQuery] DateTime? startDateTo,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var actor = GetActor();

        // 越权防护:普通员工只能查看本人成绩;HR/管理员可查任意员工。
        if (!actor.IsAdmin && !actor.IsHr)
        {
            employeeId = GetCurrentEmployeeId();
        }

        var result = await _testService.GetScoreSummariesAsync(
            employeeId, courseId, employeeName, courseName, startDateFrom, startDateTo, page, pageSize);
        return OkResponse(result, "查询成功");
    }

    /// <summary>查询某员工某课程的 PRE/POST 成绩与提升值。</summary>
    [HttpGet("improvement")]
    public async Task<ActionResult<ApiResponse<TestImprovementResponse>>> GetImprovement(
        [FromQuery] int employeeId,
        [FromQuery] int courseId)
    {
        var actor = GetActor();

        var currentEmployeeId = GetCurrentEmployeeId();
        if (!actor.IsAdmin && !actor.IsHr)
        {
            employeeId = currentEmployeeId;
        }

        var result = await _testService.GetImprovementAsync(employeeId, courseId);
        return OkResponse(result, "查询成功");
    }

    private int GetCurrentEmployeeId()
    {
        var employeeId = User.GetEmployeeId()
            ?? throw new UnauthorizedApiException("无法获取当前用户身份。");
        return (int)employeeId;
    }

    private ActorContext GetActor()
    {
        var employeeId = GetCurrentEmployeeId();
        var roles = User.FindAll(ClaimTypes.Role).Select(claim => claim.Value).ToArray();
        return new ActorContext(employeeId, roles);
    }
}
