using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TrainingManagement.Api.Common.Enums;
using TrainingManagement.Api.Common.Exceptions;
using TrainingManagement.Api.Common.Responses;
using TrainingManagement.Api.Common.Security;
using TrainingManagement.Api.Dtos.Course;
using TrainingManagement.Api.Services.Interfaces;

namespace TrainingManagement.Api.Controllers;

[Authorize]
[Route("api/courses")]
public sealed class CoursesController : ApiControllerBase
{
    private readonly ICourseService _courseService;

    public CoursesController(ICourseService courseService)
    {
        _courseService = courseService;
    }

    [HttpGet]
    [ProducesResponseType(
        typeof(ApiResponse<PagedResult<CourseResponse>>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        typeof(ApiResponse<object>),
        StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<PagedResult<CourseResponse>>>> GetAll(
        [FromQuery] CourseQuery query,
        CancellationToken cancellationToken)
    {
        if (!IsHrOrAdmin(User))
        {
            query.CourseStatus = CourseStatusText.Published;
        }

        var courses = await _courseService.GetAllAsync(
            query,
            cancellationToken);

        return OkResponse(courses);
    }

    [HttpGet("{id:long}")]
    [ProducesResponseType(
        typeof(ApiResponse<CourseResponse>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ApiResponse<object>),
        StatusCodes.Status404NotFound)]
    [ProducesResponseType(
        typeof(ApiResponse<object>),
        StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<CourseResponse>>> GetById(
        long id,
        CancellationToken cancellationToken)
    {
        var course = await _courseService.GetByIdAsync(
            id,
            cancellationToken);

        if (course is null)
        {
            throw new NotFoundApiException("课程不存在。");
        }

        if (!IsHrOrAdmin(User)
            && string.Equals(course.CourseStatus, CourseStatusText.Draft, StringComparison.OrdinalIgnoreCase))
        {
            throw new ForbiddenApiException("课程尚未发布。");
        }

        return OkResponse(course);
    }

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.HrOrAdmin)]
    [ProducesResponseType(
        typeof(ApiResponse<CourseResponse>),
        StatusCodes.Status201Created)]
    [ProducesResponseType(
        typeof(ApiResponse<object>),
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        typeof(ApiResponse<object>),
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
        typeof(ApiResponse<object>),
        StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<CourseResponse>>> Create(
        [FromBody] CreateCourseRequest request,
        CancellationToken cancellationToken)
    {
        var course = await _courseService.CreateAsync(
            request,
            cancellationToken);

        return CreatedResponse(
            nameof(GetById),
            new { id = course.CourseId },
            course);
    }

    [HttpPut("{id:long}")]
    [Authorize(Policy = AuthorizationPolicies.HrOrAdmin)]
    [ProducesResponseType(
        typeof(ApiResponse<CourseResponse>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ApiResponse<object>),
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        typeof(ApiResponse<object>),
        StatusCodes.Status404NotFound)]
    [ProducesResponseType(
        typeof(ApiResponse<object>),
        StatusCodes.Status409Conflict)]
    [ProducesResponseType(
        typeof(ApiResponse<object>),
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
        typeof(ApiResponse<object>),
        StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<CourseResponse>>> Update(
        long id,
        [FromBody] UpdateCourseRequest request,
        CancellationToken cancellationToken)
    {
        var course = await _courseService.UpdateAsync(
            id,
            request,
            cancellationToken);

        return OkResponse(course);
    }

    [HttpPatch("{id:long}/publish")]
    [Authorize(Policy = AuthorizationPolicies.HrOrAdmin)]
    [ProducesResponseType(
        typeof(ApiResponse<PublishCourseResponse>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ApiResponse<object>),
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        typeof(ApiResponse<object>),
        StatusCodes.Status404NotFound)]
    [ProducesResponseType(
        typeof(ApiResponse<object>),
        StatusCodes.Status409Conflict)]
    [ProducesResponseType(
        typeof(ApiResponse<object>),
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
        typeof(ApiResponse<object>),
        StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<PublishCourseResponse>>> Publish(
        long id,
        CancellationToken cancellationToken)
    {
        var response = await _courseService.PublishAsync(
            id,
            cancellationToken);

        return OkResponse(response);
    }

    [HttpPatch("{id:long}/close")]
    [Authorize(Policy = AuthorizationPolicies.HrOrAdmin)]
    [ProducesResponseType(
        typeof(ApiResponse<CloseCourseResponse>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ApiResponse<object>),
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        typeof(ApiResponse<object>),
        StatusCodes.Status404NotFound)]
    [ProducesResponseType(
        typeof(ApiResponse<object>),
        StatusCodes.Status409Conflict)]
    [ProducesResponseType(
        typeof(ApiResponse<object>),
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
        typeof(ApiResponse<object>),
        StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<CloseCourseResponse>>> Close(
        long id,
        CancellationToken cancellationToken)
    {
        await _courseService.CloseAsync(
            id,
            cancellationToken);

        return OkResponse(
            new CloseCourseResponse());
    }

    private static bool IsHrOrAdmin(ClaimsPrincipal user)
    {
        return user.IsInRole(RoleCodes.Hr)
            || user.IsInRole(RoleCodes.Admin);
    }
}
