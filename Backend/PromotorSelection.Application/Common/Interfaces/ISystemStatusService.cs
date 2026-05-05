namespace PromotorSelection.Application.Common.Interfaces;

public interface ISystemStatusService
{
    Task<bool> IsSystemActiveAsync(CancellationToken ct = default);
    Task<string> GetCurrentStatusMessageAsync(CancellationToken ct = default);
}