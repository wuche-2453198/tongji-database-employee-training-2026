using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TrainingManagement.Api.Common.Exceptions;
using TrainingManagement.Api.Common.Responses;
using TrainingManagement.Api.Common.Security;
using TrainingManagement.Api.Dtos.Trainer;
using TrainingManagement.Api.Services.Interfaces;

namespace TrainingManagement.Api.Controllers;

[Authorize(Policy = AuthorizationPolicies.HrOrAdmin)]
[Route("api/trainers")]
public sealed class TrainersController : ApiControllerBase
{
    private readonly ITrainerService _trainerService;

    public TrainersController(ITrainerService trainerService)
    {
        _trainerService = trainerService;
    }

    [HttpGet]
    [ProducesResponseType(
        typeof(ApiResponse<PagedResult<TrainerResponse>>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        typeof(ApiResponse<object>),
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
        typeof(ApiResponse<object>),
        StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<PagedResult<TrainerResponse>>>> GetAll(
        [FromQuery] TrainerQuery query,
        CancellationToken cancellationToken)
    {
        var trainers = await _trainerService.GetAllAsync(
            query,
            cancellationToken);

        return OkResponse(trainers);
    }

    [HttpGet("{id:long}")]
    [ProducesResponseType(
        typeof(ApiResponse<TrainerResponse>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ApiResponse<object>),
        StatusCodes.Status404NotFound)]
    [ProducesResponseType(
        typeof(ApiResponse<object>),
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
        typeof(ApiResponse<object>),
        StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<TrainerResponse>>> GetById(
        long id,
        CancellationToken cancellationToken)
    {
        var trainer = await _trainerService.GetByIdAsync(
            id,
            cancellationToken);

        if (trainer is null)
        {
            throw new NotFoundApiException("讲师不存在。");
        }

        return OkResponse(trainer);
    }

    [HttpPost]
    [ProducesResponseType(
        typeof(ApiResponse<TrainerResponse>),
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
    public async Task<ActionResult<ApiResponse<TrainerResponse>>> Create(
        [FromBody] CreateTrainerRequest request,
        CancellationToken cancellationToken)
    {
        var trainer = await _trainerService.CreateAsync(
            request,
            cancellationToken);

        return CreatedResponse(
            nameof(GetById),
            new { id = trainer.TrainerId },
            trainer);
    }

    [HttpPut("{id:long}")]
    [ProducesResponseType(
        typeof(ApiResponse<TrainerResponse>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ApiResponse<object>),
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        typeof(ApiResponse<object>),
        StatusCodes.Status404NotFound)]
    [ProducesResponseType(
        typeof(ApiResponse<object>),
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
        typeof(ApiResponse<object>),
        StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<TrainerResponse>>> Update(
        long id,
        [FromBody] UpdateTrainerRequest request,
        CancellationToken cancellationToken)
    {
        var trainer = await _trainerService.UpdateAsync(
            id,
            request,
            cancellationToken);

        return OkResponse(trainer);
    }
}
