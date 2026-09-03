using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TrainingManagement.Api.Common.Exceptions;
using TrainingManagement.Api.Common.Responses;
using TrainingManagement.Api.Common.Security;
using TrainingManagement.Api.Dtos.Registration;
using TrainingManagement.Api.Services.Interfaces;

namespace TrainingManagement.Api.Controllers;

[Authorize]
[Route("api/registrations")]
public sealed class RegistrationsController : ApiControllerBase
{
    private readonly IRegistrationService _registrationService;

    public RegistrationsController(
        IRegistrationService registrationService)
    {
        _registrationService = registrationService;
    }

    [HttpGet("my")]
    [ProducesResponseType(
        typeof(ApiResponse<PagedResult<RegistrationResponse>>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ApiResponse<object>),
        StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<PagedResult<RegistrationResponse>>>> GetMy(
        [FromQuery] RegistrationQuery query,
        CancellationToken cancellationToken)
    {
        var registrations = await _registrationService.GetMyAsync(
            query,
            User,
            cancellationToken);

        return OkResponse(registrations);
    }

    [HttpGet]
    [Authorize(Policy = AuthorizationPolicies.HrOrAdmin)]
    [ProducesResponseType(
        typeof(ApiResponse<PagedResult<RegistrationResponse>>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ApiResponse<object>),
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
        typeof(ApiResponse<object>),
        StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<PagedResult<RegistrationResponse>>>> GetAll(
        [FromQuery] RegistrationQuery query,
        CancellationToken cancellationToken)
    {
        var registrations = await _registrationService.GetAllAsync(
            query,
            User,
            cancellationToken);

        return OkResponse(registrations);
    }

    [HttpGet("summary")]
    [Authorize(Policy = AuthorizationPolicies.HrOrAdmin)]
    [ProducesResponseType(
        typeof(ApiResponse<RegistrationSummaryResponse>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ApiResponse<object>),
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
        typeof(ApiResponse<object>),
        StatusCodes.Status403Forbidden)]
    [ProducesResponseType(
        typeof(ApiResponse<object>),
        StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<RegistrationSummaryResponse>>> GetSummary(
        [FromQuery] long? courseId,
        CancellationToken cancellationToken)
    {
        var summary = await _registrationService.GetSummaryAsync(
            courseId,
            User,
            cancellationToken);

        return OkResponse(summary);
    }

    [HttpGet("{id:long}")]
    [ProducesResponseType(
        typeof(ApiResponse<RegistrationResponse>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ApiResponse<object>),
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
        typeof(ApiResponse<object>),
        StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<RegistrationResponse>>> GetById(
        long id,
        CancellationToken cancellationToken)
    {
        var registration = await _registrationService.GetByIdAsync(
            id,
            User,
            cancellationToken);

        if (registration is null)
        {
            throw new NotFoundApiException(
                "报名记录不存在。");
        }

        return OkResponse(registration);
    }

    [HttpPost]
    [ProducesResponseType(
        typeof(ApiResponse<RegistrationResponse>),
        StatusCodes.Status201Created)]
    [ProducesResponseType(
        typeof(ApiResponse<object>),
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        typeof(ApiResponse<object>),
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
        typeof(ApiResponse<object>),
        StatusCodes.Status404NotFound)]
    [ProducesResponseType(
        typeof(ApiResponse<object>),
        StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<RegistrationResponse>>> Create(
        [FromBody] CreateRegistrationRequest request,
        CancellationToken cancellationToken)
    {
        var registration = await _registrationService.CreateAsync(
            request,
            User,
            cancellationToken);

        return CreatedResponse(
            nameof(GetById),
            new
            {
                id = registration.RegId
            },
            registration);
    }

    [HttpPatch("{id:long}/cancel")]
    [ProducesResponseType(
        typeof(ApiResponse<RegistrationResponse>),
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
    public async Task<ActionResult<ApiResponse<RegistrationResponse>>> Cancel(
        long id,
        [FromBody] CancelRegistrationRequest? request,
        CancellationToken cancellationToken)
    {
        var registration = await _registrationService.CancelAsync(
            id,
            request,
            User,
            cancellationToken);

        return OkResponse(registration);
    }

    [HttpPatch("{id:long}/absent")]
    [Authorize(Policy = AuthorizationPolicies.HrOrAdmin)]
    [ProducesResponseType(
        typeof(ApiResponse<RegistrationResponse>),
        StatusCodes.Status200OK)]
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
    public async Task<ActionResult<ApiResponse<RegistrationResponse>>> MarkAbsent(
        long id,
        CancellationToken cancellationToken)
    {
        var registration = await _registrationService.MarkAbsentAsync(
            id,
            User,
            cancellationToken);

        return OkResponse(registration);
    }

    [HttpPatch("{id:long}/complete")]
    [Authorize(Policy = AuthorizationPolicies.HrOrAdmin)]
    [ProducesResponseType(
        typeof(ApiResponse<RegistrationResponse>),
        StatusCodes.Status200OK)]
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
    public async Task<ActionResult<ApiResponse<RegistrationResponse>>> Complete(
        long id,
        CancellationToken cancellationToken)
    {
        var registration = await _registrationService.CompleteAsync(
            id,
            User,
            cancellationToken);

        return OkResponse(registration);
    }
}
