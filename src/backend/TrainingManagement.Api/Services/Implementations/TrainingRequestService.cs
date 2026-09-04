using TrainingManagement.Api.Dtos.TrainingRequest;
using TrainingManagement.Api.Entities;
using TrainingManagement.Api.Repositories.Interfaces;
using TrainingManagement.Api.Services.Interfaces;

namespace TrainingManagement.Api.Services.Implementations
{
    public class TrainingRequestService: ITrainingRequestService
    {
        private readonly ITrainingRequestRepository _repository;
        // 等待以下服务接口实现后取消注释
        // private readonly IEmployeeService _employeeService;
        // private readonly ICourseService _courseService;
        // private readonly IBlacklistService _blacklistService;

        public TrainingRequestService(
            ITrainingRequestRepository repository
            // 等待以下服务接口实现后取消注释
            // IEmployeeService employeeService,
            // ICourseService courseService,
            // IBlacklistService blacklistService
        )
        {
            _repository = repository;
            // 等待以下服务接口实现后取消注释
            // _employeeService = employeeService;
            // _courseService = courseService;
            // _blacklistService = blacklistService;
        }

        public async Task<TrainingRequest> SubmitRequestAsync(int employeeId, CreateTrainingRequestDto dto)
        {
            // TODO: 等待其他模块服务接口实现后，取消注释以下校验代码，已预留位置，功能暂不启用
            /*
            // 1. 检查员工是否在职
            var employee = await _employeeService.GetByIdAsync(employeeId);
            if (employee == null || employee.Status != "ACTIVE")
            {
                throw new InvalidOperationException("员工不存在或已离职，无法提交申请。");
            }

            // 2. 检查课程是否存在且已发布
            var course = await _courseService.GetByIdAsync(dto.CourseId);
            if (course == null)
            {
                throw new InvalidOperationException("课程不存在。");
            }
            if (course.Status != "PUBLISHED")
            {
                throw new InvalidOperationException("课程尚未发布，无法申请。");
            }

            // 3. 检查课程是否已开始
            if (course.StartAt <= DateTime.Now)
            {
                throw new InvalidOperationException("课程已开始，无法申请。");
            }

            // 4. 检查员工是否在黑名单中
            if (await _blacklistService.IsInBlacklistAsync(employeeId))
            {
                throw new InvalidOperationException("您当前在黑名单中，无法申请培训。");
            }
            */

            // 原有校验         
            // 5. 检查是否有待处理的申请（防止重复提交）
            var exists = await _repository.ExistsPendingRequestAsync(employeeId, dto.CourseId);
            if (exists)
            {
                throw new InvalidOperationException("您已提交过该课程的申请，请勿重复提交。");
            }

            // 6. 创建申请实体
            var request = new TrainingRequest
            {
                EmployeeId = employeeId,
                CourseId = dto.CourseId,
                RequestReason = dto.RequestReason,
                Status = "PENDING"  // 新申请默认待审批
            };

            // 7. 插入数据库
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
            // 查询申请
            var request = await _repository.GetByIdAsync(requestId);
            if (request == null)
            {
                throw new InvalidOperationException("申请不存在。");
            }

            // 业务校验：只有 PENDING 状态才能审批
            if (request.Status != "PENDING")
            {
                throw new InvalidOperationException($"当前状态为 {request.Status}，无法进行审批操作。");
            }

            // TODO: 等待员工服务接口，补充主管部门范围校验
            /*
            // 检查审批人是否属于申请人的部门
            var approver = await _employeeService.GetByIdAsync(approverId);
            var applicant = await _employeeService.GetByIdAsync(request.EmployeeId);
            if (approver == null || applicant == null || approver.DeptId != applicant.DeptId)
            {
                throw new InvalidOperationException("您只能审批本部门员工的申请。");
            }
            */

            // 更新状态
            return await _repository.UpdateStatusAsync(requestId, "DEPT_APPROVED", approverId, comment);
        }

        public async Task<bool> DeptRejectAsync(int requestId, int approverId, string? comment)
        {
            // 查询申请
            var request = await _repository.GetByIdAsync(requestId);
            if (request == null)
            {
                throw new InvalidOperationException("申请不存在。");
            }

            // 业务校验：只有 PENDING 状态才能驳回
            if (request.Status != "PENDING")
            {
                throw new InvalidOperationException($"当前状态为 {request.Status}，无法进行驳回操作。");
            }

            // TODO: 等待员工服务接口，补充主管部门范围校验
            /*
            // 检查审批人是否属于申请人的部门
            var approver = await _employeeService.GetByIdAsync(approverId);
            var applicant = await _employeeService.GetByIdAsync(request.EmployeeId);
            if (approver == null || applicant == null || approver.DeptId != applicant.DeptId)
            {
                throw new InvalidOperationException("您只能审批本部门员工的申请。");
            }
            */

            // 更新状态
            return await _repository.UpdateStatusAsync(requestId, "DEPT_REJECTED", approverId, comment);
        }

        public async Task<bool> HrFileAsync(int requestId, int hrId)
        {
            // 查询申请
            var request = await _repository.GetByIdAsync(requestId);
            if (request == null)
            {
                throw new InvalidOperationException("申请不存在。");
            }

            // 业务校验：只有 DEPT_APPROVED 状态才能备案
            if (request.Status != "DEPT_APPROVED")
            {
                throw new InvalidOperationException($"当前状态为 {request.Status}，无法进行备案操作。");
            }

            // TODO: 等待员工服务接口，补充主管部门范围校验
            /*
            // 检查审批人是否属于申请人的部门
            var approver = await _employeeService.GetByIdAsync(approverId);
            var applicant = await _employeeService.GetByIdAsync(request.EmployeeId);
            if (approver == null || applicant == null || approver.DeptId != applicant.DeptId)
            {
                throw new InvalidOperationException("您只能审批本部门员工的申请。");
            }
            */

            // 更新状态
            return await _repository.UpdateStatusAsync(requestId, "HR_FILED", hrId, null);
        }
    }
}
