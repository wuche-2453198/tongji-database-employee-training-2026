using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq; 
using TrainingManagement.Api.Dtos.TrainingRequest;
using TrainingManagement.Api.Services.Interfaces;

namespace TrainingManagement.Api.Controllers
{
    [ApiController]                          // ← 特性
    [Route("api/[controller]")]              // ← 路由
    public class TrainingRequestsController: ControllerBase
    {
        private readonly ITrainingRequestService _service;

        public TrainingRequestsController(ITrainingRequestService service)
        {
            _service = service;
        }

        // ============================================================
        // 1. 提交申请
        // POST /api/training-requests
        // ============================================================
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateTrainingRequestDto dto)
        {
            // 参数验证
            if (!ModelState.IsValid)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "请求参数无效",
                    errors = ModelState.Values
                        .SelectMany(v => v.Errors.Select(e => e.ErrorMessage))
                });
            }

            try
            {
                var employeeId = GetCurrentEmployeeId();
                var result = await _service.SubmitRequestAsync(employeeId, dto);

                var response = MapToResponseDto(result);

                return CreatedAtAction(
                    nameof(GetById),
                    new { id = result.Id },
                    new { success = true, message = "申请提交成功", data = response }
                );
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { success = false, message = ex.Message });
            }
        }

        // ============================================================
        // 2. 员工查看自己的申请
        // GET /api/training-requests/my
        // ============================================================
        [HttpGet("my")]
        public async Task<IActionResult> GetMyRequests([FromQuery] TrainingRequestQueryDto query)
        {
            var employeeId = GetCurrentEmployeeId();
            var (items, total) = await _service.GetMyRequestsAsync(employeeId, query);

            var responseItems = items.Select(MapToResponseDto).ToList();

            return Ok(new
            {
                success = true,
                message = "ok",
                data = new
                {
                    items = responseItems,
                    page = query.Page,
                    pageSize = query.PageSize,
                    total = total
                }
            });
        }

        // ============================================================
        // 3. 主管/HR 查询所有申请
        // GET /api/training-requests
        // ============================================================
        [HttpGet]
        [Authorize(Roles = "Manager,HR")]  // 只有主管或HR可以访问
        public async Task<IActionResult> GetAll([FromQuery] TrainingRequestQueryDto query)
        {
            var (items, total) = await _service.GetAllRequestsAsync(query);

            var responseItems = items.Select(MapToResponseDto).ToList();

            return Ok(new
            {
                success = true,
                message = "ok",
                data = new
                {
                    items = responseItems,
                    page = query.Page,
                    pageSize = query.PageSize,
                    total = total
                }
            });
        }

        // ============================================================
        // 4. 查看申请详情
        // GET /api/training-requests/{id}
        // ============================================================
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _service.GetRequestByIdAsync(id);

            if (result == null)
            {
                return NotFound(new { success = false, message = "申请不存在" });
            }

            var response = MapToResponseDto(result);

            return Ok(new { success = true, message = "ok", data = response });
        }

        // ============================================================
        // 5. 主管审批通过
        // PATCH /api/training-requests/{id}/dept-approve
        // ============================================================
        [HttpPatch("{id}/dept-approve")]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> DeptApprove(int id, [FromBody] ApproveRequestDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "请求参数无效",
                    errors = ModelState.Values
                        .SelectMany(v => v.Errors.Select(e => e.ErrorMessage))
                });
            }

            try
            {
                var approverId = GetCurrentEmployeeId();
                var success = await _service.DeptApproveAsync(id, approverId, dto.Comment);

                if (!success)
                {
                    return BadRequest(new { success = false, message = "审批操作失败" });
                }

                return Ok(new { success = true, message = "审批通过" });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        // ============================================================
        // 6. 主管驳回
        // PATCH /api/training-requests/{id}/dept-reject
        // ============================================================
        [HttpPatch("{id}/dept-reject")]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> DeptReject(int id, [FromBody] ApproveRequestDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "请求参数无效",
                    errors = ModelState.Values
                        .SelectMany(v => v.Errors.Select(e => e.ErrorMessage))
                });
            }

            try
            {
                var approverId = GetCurrentEmployeeId();
                var success = await _service.DeptRejectAsync(id, approverId, dto.Comment);

                if (!success)
                {
                    return BadRequest(new { success = false, message = "驳回操作失败" });
                }

                return Ok(new { success = true, message = "已驳回" });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        // ============================================================
        // 7. HR 备案
        // PATCH /api/training-requests/{id}/hr-file
        // ============================================================
        [HttpPatch("{id}/hr-file")]
        [Authorize(Roles = "HR")]
        public async Task<IActionResult> HrFile(int id)
        {
            try
            {
                var hrId = GetCurrentEmployeeId();
                var success = await _service.HrFileAsync(id, hrId);

                if (!success)
                {
                    return BadRequest(new { success = false, message = "备案操作失败" });
                }

                return Ok(new { success = true, message = "备案完成" });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        // ============================================================
        // 辅助方法
        // ============================================================

        /// <summary>
        /// 获取当前登录用户 ID（临时实现，后续从 JWT Token 中读取）
        /// </summary>
        private int GetCurrentEmployeeId()
        {
            // TODO: 等认证模块完成后，从 HttpContext.User 中读取
            // var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            // return claim != null ? int.Parse(claim.Value) : 0;
            return 56; // 测试用固定值
        }

        /// <summary>
        /// 将 Entity 转换为 ResponseDto
        /// </summary>
        private TrainingRequestResponseDto MapToResponseDto(Entities.TrainingRequest entity)
        {
            return new TrainingRequestResponseDto
            {
                Id = entity.Id,
                EmployeeId = entity.EmployeeId,
                EmployeeName = "待实现",  // 后续通过关联查询获取
                CourseId = entity.CourseId,
                CourseName = "待实现",     // 后续通过关联查询获取
                RequestReason = entity.RequestReason,
                Status = entity.Status,
                StatusDisplay = GetStatusDisplay(entity.Status),
                DeptApproverId = entity.DeptApproverId,
                DeptApproverName = null,   // 后续通过关联查询获取
                DeptApproveTime = entity.DeptApproveTime,
                DeptApproveComment = entity.DeptApproveComment,
                HrApproverId = entity.HrApproverId,
                HrApproverName = null,     // 后续通过关联查询获取
                HrFileTime = entity.HrFileTime,
                CreateTime = entity.CreateTime
            };
        }

        /// <summary>
        /// 状态枚举转中文显示
        /// </summary>
        private string GetStatusDisplay(string status)
        {
            return status switch
            {
                "PENDING" => "待审批",
                "DEPT_APPROVED" => "主管已通过",
                "DEPT_REJECTED" => "主管已驳回",
                "HR_FILED" => "HR已备案",
                _ => status
            };
        }
    }
}
