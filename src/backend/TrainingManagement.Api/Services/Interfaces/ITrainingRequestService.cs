using TrainingManagement.Api.Dtos.TrainingRequest;
using TrainingManagement.Api.Common.Responses;

namespace TrainingManagement.Api.Services.Interfaces;

public interface ITrainingRequestService
{
    Task<TrainingRequestResponseDto> SubmitRequestAsync(
        int employeeId, CreateTrainingRequestDto dto, CancellationToken cancellationToken);

    Task<PagedResult<TrainingRequestResponseDto>> GetMyRequestsAsync(
        int employeeId, TrainingRequestQueryDto query, CancellationToken cancellationToken);

    Task<PagedResult<TrainingRequestResponseDto>> GetAllRequestsAsync(
        ActorContext actor, TrainingRequestQueryDto query, CancellationToken cancellationToken);

    Task<TrainingRequestResponseDto> GetRequestByIdAsync(
        ActorContext actor, int id, CancellationToken cancellationToken);

    Task<TrainingRequestResponseDto> DeptApproveAsync(
        ActorContext actor, int requestId, string? comment, CancellationToken cancellationToken);

    Task<TrainingRequestResponseDto> DeptRejectAsync(
        ActorContext actor, int requestId, string? comment, CancellationToken cancellationToken);

    Task<TrainingRequestResponseDto> HrFileAsync(
        ActorContext actor, int requestId, string? comment, CancellationToken cancellationToken);
}
