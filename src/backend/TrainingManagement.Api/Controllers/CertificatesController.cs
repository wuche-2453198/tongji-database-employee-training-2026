using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TrainingManagement.Api.Common.Exceptions;
using TrainingManagement.Api.Common.Extensions;
using TrainingManagement.Api.Common.Responses;
using TrainingManagement.Api.Common.Security;
using TrainingManagement.Api.Dtos.Certificates;
using TrainingManagement.Api.Dtos.TrainingRequest;
using TrainingManagement.Api.Entities;
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

    [Authorize(Roles = RoleCodes.Hr + "," + RoleCodes.Admin)]
    [HttpPost]
    public async Task<ActionResult<ApiResponse<CertificateResult>>> Generate(
        [FromBody] GenerateCertificateRequest request)
    {
        var issuedBy = GetCurrentEmployeeId();
        var result = await _certificateService.GenerateCertificateAsync(request, issuedBy);
        return CreatedResponse(nameof(GetById), new { id = result.CertificateId }, result, "证书生成成功");
    }

    [HttpGet("my")]
    public async Task<ActionResult<ApiResponse<IEnumerable<TrainingCertificate>>>> GetMyCertificates()
    {
        var employeeId = GetCurrentEmployeeId();
        var result = await _certificateService.GetMyCertificatesAsync(employeeId);
        return OkResponse(result, "查询成功");
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<TrainingCertificate>>> GetById(int id)
    {
        var actor = GetActor();
        var result = await _certificateService.GetCertificateByIdAsync(actor, id);
        return OkResponse(result, "查询成功");
    }

    [Authorize(Roles = RoleCodes.Hr + "," + RoleCodes.Admin)]
    [HttpPatch("{id:int}/notify")]
    public async Task<ActionResult<ApiResponse<bool>>> NotifyExpiry(int id)
    {
        var result = await _certificateService.MarkNotifiedAsync(id);
        return OkResponse(result, "提醒标记成功");
    }

    private int GetCurrentEmployeeId()
    {
        var employeeId = User.GetEmployeeId()
            ?? throw new UnauthorizedApiException("无法获取当前用户身份。");
        return (int)employeeId;
    }

    private ActorContext GetActor()
    {
        var employeeId = GetCurrentEmployeeId();
        var roles = User.FindAll(ClaimTypes.Role).Select(claim => claim.Value).ToArray();
        return new ActorContext(employeeId, roles);
    }
}
