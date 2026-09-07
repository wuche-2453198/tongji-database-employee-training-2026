using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TrainingManagement.Api.Common;
using TrainingManagement.Api.Dtos.Tests;
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

    [HttpPost]
    public async Task<ActionResult<ApiResponse<bool>>> Create([FromBody] CreateTestRequest request)
    {
        var result = await _testService.CreateTestAsync(request);
        return OkResponse(result, "成绩录入成功");
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<object>>> GetList(
        [FromQuery] int? employeeId,
        [FromQuery] int? courseId,
        [FromQuery] string? testType)
    {
        var result = await _testService.GetTestListAsync(employeeId, courseId, testType);
        return OkResponse(result, "查询成功");
    }
}
