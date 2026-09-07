using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TrainingManagement.Api.Common;
using TrainingManagement.Api.Common.Responses;
using TrainingManagement.Api.Dtos.Certificates;
using TrainingManagement.Api.Services.Interfaces;

namespace TrainingManagement.Api.Controllers;

[Authorize]
[Route("api/certificates")]
public sealed class CertificatesController : ApiControllerBase
{
    private readonly ICertificateService _certificateService;

    public CertificatesController(ICertificateService certificateService)
    {
        _certificateService = certificateService;
    }

    [Authorize(Roles = "HR,Admin")]
    [HttpPost]
    public async Task<ActionResult<ApiResponse<CertificateResult>>> Generate([FromBody] GenerateCertificateRequest request)
    {
        var issuedBy = GetCurrentEmployeeId();
        var result = await _certificateService.GenerateCertificateAsync(request, issuedBy);
        return OkResponse(result, "证书生成成功");
    }

    [HttpGet("my")]
    public async Task<ActionResult<ApiResponse<IEnumerable<object>>>> GetMyCertificates()
    {
        var employeeId = GetCurrentEmployeeId();
        var result = await _certificateService.GetMyCertificatesAsync(employeeId);
        return OkResponse(result, "查询成功");
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<object>>> GetById(int id)
    {
        var result = await _certificateService.GetCertificateByIdAsync(id);
        return OkResponse(result, "查询成功");
    }

    [Authorize(Roles = "HR,Admin")]
    [HttpPatch("{id}/notify")]
    public async Task<ActionResult<ApiResponse<bool>>> NotifyExpiry(int id)
    {
        var result = await _certificateService.MarkNotifiedAsync(id);
        return OkResponse(result, "提醒标记成功");
    }

    private int GetCurrentEmployeeId()
    {
        var claim = User.FindFirst("emp_id") ?? User.FindFirst(ClaimTypes.NameIdentifier);
        if (claim == null)
            throw new UnauthorizedAccessException("无法获取用户ID");
        return int.Parse(claim.Value);
    }
}
