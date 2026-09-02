using Microsoft.AspNetCore.Mvc;
using TrainingManagement.Api.Common;
using TrainingManagement.Api.Dtos.Ratings;

namespace TrainingManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RatingsController : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<ApiResponse<bool>>> Create([FromBody] CreateRatingRequest request)
    {
        // TODO: 校验评分 1-5，同一员工同一课程不能重复评分
        return Ok(ApiResponse<bool>.Success(true, "评分成功"));
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<object>>> GetList([FromQuery] int? courseId, [FromQuery] int? trainerId)
    {
        // TODO: 分页返回评分列表
        return Ok(ApiResponse<object>.Success(new { message = "列表查询待实现" }));
    }

    [HttpPatch("{id}/verify")]
    public async Task<ActionResult<ApiResponse<bool>>> Verify(int id, [FromBody] string verifyComment)
    {
        // TODO: 更新 VerifyStatus = "VERIFIED"
        return Ok(ApiResponse<bool>.Success(true, "复核完成"));
    }
}
