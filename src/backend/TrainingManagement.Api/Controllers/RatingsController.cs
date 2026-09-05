using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TrainingManagement.Api.Common;
using TrainingManagement.Api.Dtos.Ratings;
using TrainingManagement.Api.Services.Interfaces;

namespace TrainingManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RatingsController : ControllerBase
{
    private readonly IRatingService _ratingService;

    public RatingsController(IRatingService ratingService)
    {
        _ratingService = ratingService;
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<bool>>> Create([FromBody] CreateRatingRequest request)
    {
        var employeeIdClaim = User.FindFirst("emp_id") ?? User.FindFirst(ClaimTypes.NameIdentifier);
        if (employeeIdClaim == null)
        {
            return Unauthorized(ApiResponse<bool>.Fail("无法获取用户ID"));
        }

        var employeeId = int.Parse(employeeIdClaim.Value);
        var result = await _ratingService.CreateRatingAsync(request, employeeId);
        return Ok(ApiResponse<bool>.Success(result, "评分成功"));
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<object>>> GetList(
        [FromQuery] int? courseId,
        [FromQuery] int? trainerId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _ratingService.GetRatingListAsync(courseId, trainerId, page, pageSize);
        return Ok(ApiResponse<object>.Success(result, "查询成功"));
    }

    [HttpPatch("{id}/verify")]
    public async Task<ActionResult<ApiResponse<bool>>> Verify(int id, [FromBody] string verifyComment)
    {
        var result = await _ratingService.VerifyRatingAsync(id, verifyComment);
        return Ok(ApiResponse<bool>.Success(result, "复核完成"));
    }
}
