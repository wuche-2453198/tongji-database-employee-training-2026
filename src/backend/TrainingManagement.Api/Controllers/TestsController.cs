using Microsoft.AspNetCore.Mvc;
using TrainingManagement.Api.Common;
using TrainingManagement.Api.Dtos.Tests;

namespace TrainingManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TestsController : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<ApiResponse<bool>>> Create([FromBody] CreateTestRequest request)
    {
        // TODO: 校验分数 0-100，同一员工同一课程同一类型只能有一条
        return Ok(ApiResponse<bool>.Success(true, "成绩录入成功"));
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<object>>> GetList([FromQuery] int? employeeId, [FromQuery] int? courseId, [FromQuery] string? testType)
    {
        // TODO: 返回列表
        return Ok(ApiResponse<object>.Success(new { message = "查询待实现" }));
    }
}
