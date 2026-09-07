using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TrainingManagement.Api.Common;
using TrainingManagement.Api.Common.Responses;
using TrainingManagement.Api.Dtos.Ratings;
using TrainingManagement.Api.Services.Interfaces;

namespace TrainingManagement.Api.Controllers;

[Authorize]
[Route("api/ratings")]
public sealed class RatingsController : ApiControllerBase
{
    private readonly IRatingService _ratingService;

    public RatingsController(IRatingService ratingService)
    {
        _ratingService = ratingService;
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<bool>>> Create([FromBody] CreateRatingRequest request)
    {
        var employeeId = GetCurrentEmployeeId();
        var result = await _ratingService.CreateRatingAsync(request, employeeId);
        return OkResponse(result, "评分成功");
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<object>>>> GetList(
        [FromQuery] int? courseId,
        [FromQuery] int? trainerId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _ratingService.GetRatingListAsync(courseId, trainerId, page, pageSize);
        return OkResponse(result, "查询成功");
    }

    [Authorize(Roles = "HR,Admin")]
    [HttpPatch("{id}/verify")]
    public async Task<ActionResult<ApiResponse<bool>>> Verify(int id, [FromBody] string verifyComment)
    {
        var hrId = GetCurrentEmployeeId();
        var result = await _ratingService.VerifyRatingAsync(id, verifyComment, hrId);
        return OkResponse(result, "复核完成");
    }

    private int GetCurrentEmployeeId()
    {
        var claim = User.FindFirst("emp_id") ?? User.FindFirst(ClaimTypes.NameIdentifier);
        if (claim == null)
            throw new UnauthorizedAccessException("无法获取用户ID");
        return int.Parse(claim.Value);
    }
}
