namespace PromotorSelection.Application.Common.Interfaces;

public interface ISystemStatusService
{
    Task<bool> IsSystemActiveAsync(CancellationToken ct = default);
    Task<string> GetCurrentStatusMessageAsync(CancellationToken ct = default);
    Task<DateTime?> GetStartDateAsync(CancellationToken ct = default);
    Task<DateTime?> GetEndDateAsync(CancellationToken ct = default);
}