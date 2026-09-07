using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TrainingManagement.Api.Common.Exceptions;
using TrainingManagement.Api.Dtos.Certificates;
using TrainingManagement.Api.Entities;
using TrainingManagement.Api.Repositories.Interfaces;
using TrainingManagement.Api.Services.Interfaces;

namespace TrainingManagement.Api.Services.Implementations;
public sealed class CertificateService : ICertificateService
{
    private readonly ICertificateRepository _certificateRepository;

    public CertificateService(ICertificateRepository certificateRepository)
    {
        _certificateRepository = certificateRepository;
    }

    public async Task<CertificateResult> GenerateCertificateAsync(GenerateCertificateRequest request, int issuedByEmpId)
    {
        var registration = await _certificateRepository.GetRegistrationByIdAsync(request.RegistrationId);
        if (registration == null)
            throw new NotFoundException("报名记录不存在");

        if (registration.Status != "COMPLETED")
            throw new ConflictException("该员工尚未完成培训，无法生成证书");

        var exists = await _certificateRepository.ExistsByEmployeeAndCourseAsync(registration.EmpId, registration.CourseId);
        if (exists)
            throw new ConflictException("该员工已获得该课程的证书，不能重复生成");

        var certCode = GenerateCertificateCode(registration.CourseId, registration.EmpId);
        var certId = await _certificateRepository.CreateAsync(certCode, registration.EmpId, registration.CourseId, issuedByEmpId);

        return new CertificateResult { CertificateId = certId, CertificateCode = certCode };
    }

    private static string GenerateCertificateCode(int courseId, int employeeId)
    {
        var today = DateTime.Now.ToString("yyyyMMdd");
        return $"CERT-{today}-{courseId}-{employeeId}";
    }

    public async Task<IEnumerable<TrainingCertificate>> GetMyCertificatesAsync(int employeeId)
    {
        return await _certificateRepository.GetByEmployeeIdAsync(employeeId);
    }

    public async Task<TrainingCertificate> GetCertificateByIdAsync(int id)
    {
        var cert = await _certificateRepository.GetByIdAsync(id);
        if (cert == null)
            throw new NotFoundException("证书不存在");
        return cert;
    }

    public async Task<bool> MarkNotifiedAsync(int id)
    {
        var cert = await _certificateRepository.GetByIdAsync(id);
        if (cert == null)
            throw new NotFoundException("证书不存在");

        if (cert.Notified == "Y")
            throw new ConflictException("该证书已经标记过提醒了");

        return await _certificateRepository.UpdateNotifyFlagAsync(id);
    }
}
