using LawncareApi.Models;

namespace LawncareApi.Services;

/// <summary>Abstracts lawn care task persistence.</summary>
public interface ILawnCareTaskService
{
    Task<IReadOnlyList<LawnCareTask>> GetAllAsync(CancellationToken ct = default);
    Task<LawnCareTask?> GetByIdAsync(string id, CancellationToken ct = default);
    Task<LawnCareTask> CreateAsync(LawnCareTaskRequest request, CancellationToken ct = default);
    Task<LawnCareTask?> UpdateAsync(string id, LawnCareTaskRequest request, CancellationToken ct = default);
    Task<bool> DeleteAsync(string id, CancellationToken ct = default);
}
