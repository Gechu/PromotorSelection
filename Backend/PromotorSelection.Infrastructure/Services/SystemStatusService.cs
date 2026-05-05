using Microsoft.EntityFrameworkCore;
using PromotorSelection.Application.Common.Interfaces;

namespace PromotorSelection.Infrastructure.Services;

public class SystemStatusService : ISystemStatusService
{
    private readonly IApplicationDbContext _context;

    public SystemStatusService(IApplicationDbContext context) => _context = context;

    public async Task<bool> IsSystemActiveAsync(CancellationToken ct = default)
    {
        var schedule = await _context.Schedules.FirstOrDefaultAsync(ct);
        if (schedule == null) return false;

        var now = DateTime.Now;
        return now >= schedule.StartDate && now <= schedule.EndDate;
    }

    public async Task<string> GetCurrentStatusMessageAsync(CancellationToken ct = default)
    {
        var schedule = await _context.Schedules.FirstOrDefaultAsync(ct);
        if (schedule == null) return "Harmonogram nie został ustalony.";

        var now = DateTime.Now;
        if (now < schedule.StartDate) return $"System zostanie otwarty: {schedule.StartDate}";
        if (now > schedule.EndDate) return "System został zamknięty.";
        return "System jest aktywny.";
    }
}