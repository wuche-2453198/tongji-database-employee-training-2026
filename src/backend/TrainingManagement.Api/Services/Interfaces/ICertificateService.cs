using System.Collections.Generic;
using System.Threading.Tasks;
using TrainingManagement.Api.Dtos.Certificates;
using TrainingManagement.Api.Entities;

namespace TrainingManagement.Api.Services.Interfaces;
public interface ICertificateService
{
    Task<CertificateResult> GenerateCertificateAsync(GenerateCertificateRequest request, int issuedByEmpId);
    Task<IEnumerable<TrainingCertificate>> GetMyCertificatesAsync(int employeeId);
    Task<TrainingCertificate> GetCertificateByIdAsync(int id);
    Task<bool> MarkNotifiedAsync(int id);
}
