using MediatR;
using Microsoft.EntityFrameworkCore;
using PromotorSelection.Application.Common.Interfaces;

namespace PromotorSelection.Application.Teams;

public record JoinTeamCommand(int TeamId) : IRequest<bool>;

public class JoinTeamHandler : IRequestHandler<JoinTeamCommand, bool>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly ISystemStatusService _statusService;

    public JoinTeamHandler(IApplicationDbContext context, ICurrentUserService currentUser, ISystemStatusService statusService)
    {
        _context = context;
        _currentUser = currentUser;
        _statusService = statusService;
    }

    public async Task<bool> Handle(JoinTeamCommand request, CancellationToken ct)
    {
        if (!await _statusService.IsSystemActiveAsync(ct))
            throw new Exception("Modyfikacja danych jest możliwa tylko w wyznaczonym terminie.");

        var team = await _context.Teams.Include(t => t.Members).FirstOrDefaultAsync(t => t.Id == request.TeamId, ct);

        if (team == null || team.TeamSize == -1)
            throw new Exception("Ten zespół jest zamknięty lub nie istnieje.");

        var userId = _currentUser.UserId!;
        var student = await _context.Students.FirstOrDefaultAsync(s => s.UserId == userId, ct);

        if (student == null || student.TeamId != null)
            throw new Exception("Już należysz do zespołu lub student nie istnieje.");

        if (team.Members.Count >= team.TeamSize)
            throw new Exception("Zespół osiągnął już swój limit.");

        student.TeamId = team.Id;

        if (team.Members.Count + 1 == team.TeamSize)
        {
            team.TeamSize = -1;
        }

        var allStudents = team.Members.ToList();
        allStudents.Add(student);
        var topStudent = allStudents.OrderByDescending(s => s.GradeAverage ?? 0).First();
        team.LeaderId = topStudent.UserId;

        await _context.SaveChangesAsync(ct);
        return true;
    }
}