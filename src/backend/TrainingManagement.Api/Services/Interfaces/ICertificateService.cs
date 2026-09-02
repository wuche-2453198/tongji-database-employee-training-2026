using System.Threading.Tasks;
using TrainingManagement.Api.Dtos.Certificates;

namespace TrainingManagement.Api.Services.Interfaces
{
    public interface ICertificateService
    {
        Task<object> GenerateCertificateAsync(GenerateCertificateRequest request);
        Task<object> GetMyCertificatesAsync(int employeeId);
        Task<object> GetCertificateByIdAsync(int id);
        Task<bool> MarkNotifiedAsync(int id);
    }
}
