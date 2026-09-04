using TrainingManagement.Api.Entities;

namespace TrainingManagement.Api.Repositories.Interfaces
{
    public interface ITrainingRequestRepository
    {
        Task<int> InsertAsync(TrainingRequest request);

        Task<TrainingRequest?> GetByIdAsync(int id);
        Task<(List<TrainingRequest> Items, int Total)> GetListAsync(
            string? status,
            int? employeeId,
            int? courseId,
            int page,
            int pageSize);
        Task<bool> UpdateStatusAsync(
            int id,
            string newStatus,
            int? approverId,
            string? comment);
        
        Task<bool> ExistsPendingRequestAsync(int employeeId, int courseId);
    }
}
