using TrainingManagement.Api.Common.Responses;
using TrainingManagement.Api.Dtos.Certificates;
using TrainingManagement.Api.Dtos.TrainingRequest;
using TrainingManagement.Api.Entities;

namespace TrainingManagement.Api.Services.Interfaces;

public interface ICertificateService
{
    Task<CertificateResult> GenerateCertificateAsync(GenerateCertificateRequest request, int issuedByEmpId);

    Task<IEnumerable<TrainingCertificate>> GetMyCertificatesAsync(int employeeId);

    Task<PagedResult<TrainingCertificate>> GetManagedCertificatesAsync(CertificateQueryDto query);

    Task<PagedResult<CertificateCandidate>> GetCertificateCandidatesAsync(CertificateQueryDto query);

    Task<TrainingCertificate> GetCertificateByIdAsync(ActorContext actor, int id);

    Task<bool> MarkNotifiedAsync(int id);
}
