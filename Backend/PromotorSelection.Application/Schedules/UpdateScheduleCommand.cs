using MediatR;
using Microsoft.EntityFrameworkCore;
using PromotorSelection.Application.Common.Interfaces;

namespace PromotorSelection.Application.Schedules;

public record UpdateScheduleCommand(DateTime StartDate, DateTime EndDate) : IRequest<bool>;

public class UpdateScheduleHandler : IRequestHandler<UpdateScheduleCommand, bool>
{
    private readonly IApplicationDbContext _context;

    public UpdateScheduleHandler(IApplicationDbContext context) => _context = context;

    public async Task<bool> Handle(UpdateScheduleCommand request, CancellationToken ct)
    {
        if (request.StartDate >= request.EndDate)
            throw new Exception("Data rozpoczęcia musi być przed datą zakończenia.");

        var schedule = await _context.Schedules.FirstOrDefaultAsync(ct);

        if (schedule == null)
        {
            _context.Schedules.Add(new Domain.Entities.Schedule
            {
                StartDate = request.StartDate,
                EndDate = request.EndDate
            });
        }
        else
        {
            schedule.StartDate = request.StartDate;
            schedule.EndDate = request.EndDate;
        }

        await _context.SaveChangesAsync(ct);
        return true;
    }
}