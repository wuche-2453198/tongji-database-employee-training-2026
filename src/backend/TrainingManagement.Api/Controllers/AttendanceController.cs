using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TrainingManagement.Api.Common.Responses;
using TrainingManagement.Api.Common.Security;
using TrainingManagement.Api.Dtos.Attendance;
using TrainingManagement.Api.Services.Interfaces;

namespace TrainingManagement.Api.Controllers;

[Authorize(Policy = AuthorizationPolicies.HrOrAdmin)]
[Route("api/attendance")]
public sealed class AttendanceController : ApiControllerBase
{
    private readonly IAttendanceService _attendanceService;

    public AttendanceController(
        IAttendanceService attendanceService)
    {
        _attendanceService = attendanceService;
    }

    [HttpPost("manual")]
    [ProducesResponseType(
        typeof(ApiResponse<AttendanceResponse>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ApiResponse<object>),
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        typeof(ApiResponse<object>),
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
        typeof(ApiResponse<object>),
        StatusCodes.Status403Forbidden)]
    [ProducesResponseType(
        typeof(ApiResponse<object>),
        StatusCodes.Status404NotFound)]
    [ProducesResponseType(
        typeof(ApiResponse<object>),
        StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<AttendanceResponse>>> Manual(
        [FromBody] CreateManualAttendanceRequest request,
        CancellationToken cancellationToken)
    {
        var attendance = await _attendanceService.ManualSignInAsync(
            request,
            User,
            cancellationToken);

        return OkResponse(attendance);
    }
}
