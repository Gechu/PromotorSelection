using MediatR;
using Microsoft.EntityFrameworkCore;
using PromotorSelection.Application.Common.Interfaces;

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
            throw new Exception("Modyfikacja danych jest możliwa tylko w wyznaczonym terminie.");

        var userId = _currentUser.UserId ?? throw new Exception("Nieautoryzowany dostęp.");

        var student = await _context.Students
            .Include(s => s.Team)
            .ThenInclude(t => t.Members)
            .FirstOrDefaultAsync(s => s.UserId == userId, ct);

        if (student == null)
            throw new Exception("Nie znaleziono profilu studenta.");

        student.GradeAverage = request.NewGrade;

        if (student.Team != null)
        {
            var topStudent = student.Team.Members.OrderByDescending(s => s.GradeAverage ?? 0).First();
            student.Team.LeaderId = topStudent.UserId;
        }

        await _context.SaveChangesAsync(ct);
        return true;
    }
}