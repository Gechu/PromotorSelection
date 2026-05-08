using MediatR;
using Microsoft.EntityFrameworkCore;
using PromotorSelection.Application.Common.Interfaces;
using PromotorSelection.Application.Common.Exceptions;

namespace PromotorSelection.Application.Schedules;

public record UpdateScheduleCommand(DateTime StartDate, DateTime EndDate) : IRequest<bool>;

public class UpdateScheduleHandler : IRequestHandler<UpdateScheduleCommand, bool>
{
    private readonly IApplicationDbContext _context;

    public UpdateScheduleHandler(IApplicationDbContext context) => _context = context;

    public async Task<bool> Handle(UpdateScheduleCommand request, CancellationToken ct)
    {
        var now = DateTime.Now;

        if (request.StartDate < now.AddMinutes(-1)) 
            throw new BadRequestException("Data rozpoczęcia nie może być datą przeszłą.");

        if (request.StartDate >= request.EndDate)
            throw new BadRequestException("Data rozpoczęcia musi być wcześniejsza niż data zakończenia.");

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