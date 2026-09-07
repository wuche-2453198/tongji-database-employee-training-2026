using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using TrainingManagement.Api.Common.Responses;
using TrainingManagement.Api.Dtos.Tests;
using TrainingManagement.Api.Services.Interfaces;

namespace TrainingManagement.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TestsController : ControllerBase
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
            return Ok(ApiResponse<bool>.Success(result, "成绩录入成功"));
        }

        [HttpGet]
        public async Task<ActionResult<ApiResponse<object>>> GetList([FromQuery] int? employeeId, [FromQuery] int? courseId, [FromQuery] string? testType)
        {
            var result = await _testService.GetTestListAsync(employeeId, courseId, testType);
            return Ok(ApiResponse<object>.Success(result, "查询成功"));
        }
    }
}
