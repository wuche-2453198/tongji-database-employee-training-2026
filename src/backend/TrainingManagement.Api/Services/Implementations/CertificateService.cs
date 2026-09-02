using System;
using System.Threading.Tasks;
using TrainingManagement.Api.Dtos.Certificates;
using TrainingManagement.Api.Repositories.Interfaces;
using TrainingManagement.Api.Services.Interfaces;

namespace TrainingManagement.Api.Services.Implementations
{
    public class CertificateService : ICertificateService
    {
        private readonly ICertificateRepository _certificateRepository;

        public CertificateService(ICertificateRepository certificateRepository)
        {
            _certificateRepository = certificateRepository;
        }

        public async Task<object> GenerateCertificateAsync(GenerateCertificateRequest request)
        {
            var registration = await _certificateRepository.GetRegistrationByIdAsync(request.RegistrationId);
            if (registration == null)
            {
                throw new ArgumentException("报名记录不存在");
            }

            var statusProperty = registration.GetType().GetProperty("Status");
            var status = statusProperty?.GetValue(registration)?.ToString();
            if (status != "COMPLETED")
            {
                throw new InvalidOperationException("该员工尚未完成培训，无法生成证书");
            }

            var empIdProp = registration.GetType().GetProperty("EmployeeId");
            var courseIdProp = registration.GetType().GetProperty("CourseId");
            int employeeId = Convert.ToInt32(empIdProp?.GetValue(registration));
            int courseId = Convert.ToInt32(courseIdProp?.GetValue(registration));

            var exists = await _certificateRepository.ExistsByEmployeeAndCourseAsync(employeeId, courseId);
            if (exists)
            {
                throw new InvalidOperationException("该员工已获得该课程的证书，不能重复生成");
            }

            var certificateNo = GenerateCertificateNo(courseId, employeeId);

            var result = await _certificateRepository.CreateAsync(
                certificateNo,
                employeeId,
                courseId,
                request.RegistrationId);

            return new { CertificateId = result, CertificateNo = certificateNo };
        }

        private string GenerateCertificateNo(int courseId, int employeeId)
        {
            var today = DateTime.Now.ToString("yyyyMMdd");
            return $"CERT-{today}-{courseId}-{employeeId}";
        }

        public async Task<object> GetMyCertificatesAsync(int employeeId)
        {
            return await _certificateRepository.GetByEmployeeIdAsync(employeeId);
        }

        public async Task<object> GetCertificateByIdAsync(int id)
        {
            var certificate = await _certificateRepository.GetByIdAsync(id);
            if (certificate == null)
            {
                throw new ArgumentException("证书不存在");
            }
            return certificate;
        }

        public async Task<bool> MarkNotifiedAsync(int id)
        {
            var certificate = await _certificateRepository.GetByIdAsync(id);
            if (certificate == null)
            {
                throw new ArgumentException("证书不存在");
            }

            var flagProp = certificate.GetType().GetProperty("NotifyFlag");
            var flag = flagProp?.GetValue(certificate)?.ToString();
            if (flag == "Y")
            {
                throw new InvalidOperationException("该证书已经标记过提醒了");
            }

            return await _certificateRepository.UpdateNotifyFlagAsync(id);
        }
    }
}
