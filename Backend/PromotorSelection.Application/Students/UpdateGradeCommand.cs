using MediatR;
using Microsoft.EntityFrameworkCore;
using PromotorSelection.Application.Common.Interfaces;
using PromotorSelection.Application.Common.Exceptions;

namespace PromotorSelection.Application.Students;

public record UpdateGradeCommand(double NewGrade) : IRequest<bool>;

public class UpdateGradeHandler : IRequestHandler<UpdateGradeCommand, bool>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly ISystemStatusService _statusService;

    public UpdateGradeHandler(IApplicationDbContext context, ICurrentUserService currentUser, ISystemStatusService statusService)
    {
        _context = context;
        _currentUser = currentUser;
        _statusService = statusService;
    }

    public async Task<bool> Handle(UpdateGradeCommand request, CancellationToken ct)
    {
        if (!await _statusService.IsSystemActiveAsync(ct))
            throw new BadRequestException("Modyfikacja średniej ocen jest możliwa tylko w wyznaczonym terminie trwania wyborów.");

        var userId = _currentUser.UserId ?? throw new BadRequestException("Błąd autoryzacji: Brak identyfikatora użytkownika.");

        var student = await _context.Students
            .Include(s => s.Team)
            .ThenInclude(t => t.Members)
            .FirstOrDefaultAsync(s => s.UserId == userId, ct);

        if (student == null)
            throw new NotFoundException("Nie znaleziono profilu studenta dla zalogowanego użytkownika.");

        var hasAssignment = await _context.Assignments.AnyAsync(a => a.StudentId == userId, ct);
        if (hasAssignment)
            throw new BadRequestException("Nie można zmienić średniej ocen po dokonaniu przydziału do promotora.");

        student.GradeAverage = request.NewGrade;

        if (student.Team != null)
        {
            var topStudent = student.Team.Members
                .OrderByDescending(s => s.GradeAverage ?? 0)
                .ThenBy(s => s.UserId) 
                .FirstOrDefault();

            if (topStudent != null)
            {
                student.Team.LeaderId = topStudent.UserId;
            }
        }

        await _context.SaveChangesAsync(ct);
        return true;
    }
}