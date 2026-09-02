using Microsoft.AspNetCore.Mvc;
using TrainingManagement.Api.Common;
using TrainingManagement.Api.Dtos.Certificates;

namespace TrainingManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CertificatesController : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<ApiResponse<object>>> Generate([FromBody] GenerateCertificateRequest request)
    {
        // TODO: 校验报名状态为 COMPLETED，生成 CERT-{yyyyMMdd}-{courseId}-{empId}
        return Ok(ApiResponse<object>.Success(new { CertificateNo = "CERT-20260902-1-1" }, "证书生成成功"));
    }

    [HttpGet("my")]
    public async Task<ActionResult<ApiResponse<object>>> GetMyCertificates()
    {
        // TODO: 从Token获取当前员工ID，返回列表
        return Ok(ApiResponse<object>.Success(new { message = "我的证书待实现" }));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<object>>> GetById(int id)
    {
        // TODO: 返回证书详细信息
        return Ok(ApiResponse<object>.Success(new { CertificateId = id, CourseName = "示例课程" }));
    }

    [HttpPatch("{id}/notify")]
    public async Task<ActionResult<ApiResponse<bool>>> NotifyExpiry(int id)
    {
        // TODO: 更新 NotifyFlag = "Y"
        return Ok(ApiResponse<bool>.Success(true, "提醒标记成功"));
    }
}
