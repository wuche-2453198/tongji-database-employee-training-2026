using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TrainingManagement.Api.Common.Exceptions;
using TrainingManagement.Api.Dtos.Certificates;
using TrainingManagement.Api.Dtos.TrainingRequest;
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
            throw new NotFoundApiException("报名记录不存在");

        if (registration.Status != "COMPLETED")
            throw new ConflictApiException("该员工尚未完成培训，无法生成证书");

        var exists = await _certificateRepository.ExistsByEmployeeAndCourseAsync(registration.EmpId, registration.CourseId);
        if (exists)
            throw new ConflictApiException("该员工已获得该课程的证书，不能重复生成");

        // 课程配置了训后测试时,要求 POST 成绩 >= 60(首期固定业务决策)。
        var course = await _certificateRepository.GetCourseGateAsync(registration.CourseId);
        if (!course.Exists)
            throw new NotFoundApiException("课程不存在");

        if (!string.IsNullOrWhiteSpace(course.PostTestUrl))
        {
            var postScore = await _certificateRepository.GetPostTestScoreAsync(registration.EmpId, registration.CourseId);
            if (!postScore.HasValue)
                throw new BusinessException("课程配置了训后测试，尚未录入POST成绩，无法生成证书");

            if (postScore.Value < 60m)
                throw new BusinessException("POST成绩未达到60分，无法生成证书");
        }

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

    public async Task<TrainingCertificate> GetCertificateByIdAsync(ActorContext actor, int id)
    {
        var cert = await _certificateRepository.GetByIdAsync(id);
        if (cert == null)
            throw new NotFoundApiException("证书不存在");

        // 越权防护:员工只能查看本人证书,HR/管理员可查全部。
        if (!actor.IsAdmin && !actor.IsHr && cert.EmpId != actor.EmployeeId)
            throw new ForbiddenApiException("您只能查看本人的证书");

        return cert;
    }

    public async Task<bool> MarkNotifiedAsync(int id)
    {
        var cert = await _certificateRepository.GetByIdAsync(id);
        if (cert == null)
            throw new NotFoundApiException("证书不存在");

        if (cert.Notified == "Y")
            throw new ConflictApiException("该证书已经标记过提醒了");

        return await _certificateRepository.UpdateNotifyFlagAsync(id);
    }
}
