using TrainingManagement.Api.Dtos.TrainingRequest;
using TrainingManagement.Api.Entities;

namespace TrainingManagement.Api.Services.Interfaces
{
    public interface ITrainingRequestService
    {
        // 提交申请
        Task<TrainingRequest> SubmitRequestAsync(int employeeId, CreateTrainingRequestDto dto);

        // 获取我的申请（员工）
        Task<(List<TrainingRequest> Items, int Total)> GetMyRequestsAsync(int employeeId, TrainingRequestQueryDto query);

        // 获取所有申请（主管/HR）
        Task<(List<TrainingRequest> Items, int Total)> GetAllRequestsAsync(TrainingRequestQueryDto query);

        // 根据ID获取申请详情
        Task<TrainingRequest?> GetRequestByIdAsync(int id);

        // 主管审批通过
        Task<bool> DeptApproveAsync(int requestId, int approverId, string? comment);

        // 主管驳回
        Task<bool> DeptRejectAsync(int requestId, int approverId, string? comment);

        // HR备案
        Task<bool> HrFileAsync(int requestId, int hrId);
    }
}
