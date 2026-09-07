using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Security.Claims;
using TrainingManagement.Api.Common;
using TrainingManagement.Api.Common.Responses;
using TrainingManagement.Api.Dtos.Tests;
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

    [Authorize(Roles = "HR,Admin")]
    [HttpPost]
    public async Task<ActionResult<ApiResponse<bool>>> Create([FromBody] CreateTestRequest request)
    {
        var recorderId = GetCurrentEmployeeId();
        var result = await _testService.CreateTestAsync(request, recorderId);
        return OkResponse(result, "成绩录入成功");
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<TrainingTest>>>> GetList(
        [FromQuery] int? employeeId,
        [FromQuery] int? courseId,
        [FromQuery] string? testType)
    {
        var result = await _testService.GetTestListAsync(employeeId, courseId, testType);
        return OkResponse(result, "查询成功");
    }

    private int GetCurrentEmployeeId()
    {
        var claim = User.FindFirst("emp_id") ?? User.FindFirst(ClaimTypes.NameIdentifier);
        if (claim == null)
            throw new UnauthorizedAccessException("无法获取用户ID");
        return int.Parse(claim.Value);
    }
}
