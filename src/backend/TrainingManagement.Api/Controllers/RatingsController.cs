using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TrainingManagement.Api.Common.Exceptions;
using TrainingManagement.Api.Common.Extensions;
using TrainingManagement.Api.Common.Responses;
using TrainingManagement.Api.Common.Security;
using TrainingManagement.Api.Dtos.Ratings;
using TrainingManagement.Api.Dtos.TrainingRequest;
using TrainingManagement.Api.Entities;
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
    public async Task<ActionResult<ApiResponse<PagedResult<TrainerRating>>>> GetList(
        [FromQuery] int? courseId,
        [FromQuery] int? trainerId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _ratingService.GetRatingListAsync(courseId, trainerId, page, pageSize);
        return OkResponse(result, "查询成功");
    }

    /// <summary>按课程(可选讲师)查询平均评分。</summary>
    [HttpGet("average")]
    public async Task<ActionResult<ApiResponse<RatingAverageResponse>>> GetAverage(
        [FromQuery] int courseId,
        [FromQuery] int? trainerId)
    {
        var result = await _ratingService.GetAverageAsync(courseId, trainerId);
        return OkResponse(result, "查询成功");
    }

    [Authorize(Roles = RoleCodes.Hr + "," + RoleCodes.Admin)]
    [HttpPatch("{id:int}/verify")]
    public async Task<ActionResult<ApiResponse<bool>>> Verify(
        int id,
        [FromBody] VerifyRatingRequest request)
    {
        var hrVerifierEmpId = GetCurrentEmployeeId();
        var result = await _ratingService.VerifyRatingAsync(id, request.VerifyComment, hrVerifierEmpId);
        return OkResponse(result, "复核完成");
    }

    private int GetCurrentEmployeeId()
    {
        var employeeId = User.GetEmployeeId()
            ?? throw new UnauthorizedApiException("无法获取当前用户身份。");
        return (int)employeeId;
    }
}
