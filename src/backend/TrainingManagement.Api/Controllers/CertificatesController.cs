using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;
using TrainingManagement.Api.Common;
using TrainingManagement.Api.Dtos.Certificates;
using TrainingManagement.Api.Services.Interfaces;

namespace TrainingManagement.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CertificatesController : ControllerBase
    {
        private readonly ICertificateService _certificateService;

        public CertificatesController(ICertificateService certificateService)
        {
            _certificateService = certificateService;
        }

        [HttpPost]
        public async Task<ActionResult<ApiResponse<object>>> Generate([FromBody] GenerateCertificateRequest request)
        {
            var result = await _certificateService.GenerateCertificateAsync(request);
            return Ok(ApiResponse<object>.Success(result, "证书生成成功"));
        }

        [HttpGet("my")]
        public async Task<ActionResult<ApiResponse<object>>> GetMyCertificates()
        {
            var employeeIdClaim = User.FindFirst("emp_id") ?? User.FindFirst(ClaimTypes.NameIdentifier);
            if (employeeIdClaim == null)
            {
                return Unauthorized(ApiResponse<bool>.Fail("无法获取用户ID"));
            }

            var employeeId = int.Parse(employeeIdClaim.Value);
            var result = await _certificateService.GetMyCertificatesAsync(employeeId);
            return Ok(ApiResponse<object>.Success(result, "查询成功"));
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<object>>> GetById(int id)
        {
            var result = await _certificateService.GetCertificateByIdAsync(id);
            return Ok(ApiResponse<object>.Success(result, "查询成功"));
        }

        [HttpPatch("{id}/notify")]
        public async Task<ActionResult<ApiResponse<bool>>> NotifyExpiry(int id)
        {
            var result = await _certificateService.MarkNotifiedAsync(id);
            return Ok(ApiResponse<bool>.Success(result, "提醒标记成功"));
        }
    }
}
