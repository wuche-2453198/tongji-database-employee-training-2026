using TrainingManagement.Api.Dtos.TrainingRequest;
using TrainingManagement.Api.Entities;
using TrainingManagement.Api.Repositories.Interfaces;
using TrainingManagement.Api.Services.Interfaces;

namespace TrainingManagement.Api.Services.Implementations
{
    public class TrainingRequestService: ITrainingRequestService
    {
        private readonly ITrainingRequestRepository _repository;
        public TrainingRequestService(ITrainingRequestRepository repository)
        {
            _repository = repository;
        }

        public async Task<TrainingRequest> SubmitRequestAsync(int employeeId, CreateTrainingRequestDto dto)
        {
            // 1. 检查是否有待处理的申请（防止重复提交）
            var exists = await _repository.ExistsPendingRequestAsvnc(employeeId, dto.CourseId);
            if (exists)
            {
                throw new InvalidOperationException("您已提交过该课程的申请，请勿重复提交。");
            }

            // 2. 创建申请实体
            var request = new TrainingRequest
            {
                EmployeeId = employeeId,
                CourseId = dto.CourseId,
                RequestReason = dto.RequestReason,
                Status = "PENDING"  // 新申请默认待审批
            };

            // 3. 插入数据库
            var newId = await _repository.InsertAsync(request);
            request.Id = newId;

            return request;
        }

        public async Task<(List<TrainingRequest> Items, int Total)> GetMyRequestsAsync(int employeeId, TrainingRequestQueryDto query)
        {
            // 直接调用 Repository，强制按当前员工 ID 筛选
            return await _repository.GetListAsync(
                query.Status,
                employeeId,  // 强制使用当前登录员工 ID
                query.CourseId,
                query.Page,
                query.PageSize
            );
        }

        public async Task<(List<TrainingRequest> Items, int Total)> GetAllRequestsAsync(TrainingRequestQueryDto query)
        {
            // 直接调用 Repository，所有筛选条件由客户端传入
            return await _repository.GetListAsync(
                query.Status,
                query.EmployeeId,
                query.CourseId,
                query.Page,
                query.PageSize
            );
        }

        public async Task<TrainingRequest?> GetRequestByIdAsync(int id)
        {
            return await _repository.GetByIdAsync(id);
        }

        public async Task<bool> DeptApproveAsync(int requestId, int approverId, string? comment)
        {
            // 1. 查询申请
            var request = await _repository.GetByIdAsync(requestId);
            if (request == null)
            {
                throw new InvalidOperationException("申请不存在。");
            }

            // 2. 业务校验：只有 PENDING 状态才能审批
            if (request.Status != "PENDING")
            {
                throw new InvalidOperationException($"当前状态为 {request.Status}，无法进行审批操作。");
            }

            // 3. 更新状态
            return await _repository.UpdateStatusAsync(requestId, "DEPT_APPROVED", approverId, comment);
        }

        public async Task<bool> DeptRejectAsync(int requestId, int approverId, string? comment)
        {
            // 1. 查询申请
            var request = await _repository.GetByIdAsync(requestId);
            if (request == null)
            {
                throw new InvalidOperationException("申请不存在。");
            }

            // 2. 业务校验：只有 PENDING 状态才能驳回
            if (request.Status != "PENDING")
            {
                throw new InvalidOperationException($"当前状态为 {request.Status}，无法进行驳回操作。");
            }

            // 3. 更新状态
            return await _repository.UpdateStatusAsync(requestId, "DEPT_REJECTED", approverId, comment);
        }

        public async Task<bool> HrFileAsync(int requestId, int hrId)
        {
            // 1. 查询申请
            var request = await _repository.GetByIdAsync(requestId);
            if (request == null)
            {
                throw new InvalidOperationException("申请不存在。");
            }

            // 2. 业务校验：只有 DEPT_APPROVED 状态才能备案
            if (request.Status != "DEPT_APPROVED")
            {
                throw new InvalidOperationException($"当前状态为 {request.Status}，无法进行备案操作。");
            }

            // 3. 更新状态
            return await _repository.UpdateStatusAsync(requestId, "HR_FILED", hrId, null);
        }
    }
}
