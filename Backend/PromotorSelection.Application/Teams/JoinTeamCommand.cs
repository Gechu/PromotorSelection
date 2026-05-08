using MediatR;
using Microsoft.EntityFrameworkCore;
using PromotorSelection.Application.Common.Interfaces;
using PromotorSelection.Application.Common.Exceptions;

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
            throw new BadRequestException("Dołączanie do zespołu jest możliwe tylko w wyznaczonym terminie.");

        var team = await _context.Teams
            .Include(t => t.Members)
            .FirstOrDefaultAsync(t => t.Id == request.TeamId, ct);

        if (team == null)
            throw new NotFoundException($"Zespół o ID {request.TeamId} nie istnieje.");

        if (team.TeamSize == -1)
            throw new BadRequestException("Ten zespół został już zamknięty i nie przyjmuje nowych członków.");

        var userId = _currentUser.UserId ?? throw new BadRequestException("Błąd autoryzacji.");

        var student = await _context.Students
            .FirstOrDefaultAsync(s => s.UserId == userId, ct);

        if (student == null)
            throw new NotFoundException("Nie znaleziono profilu studenta.");

        if (student.TeamId != null)
            throw new BadRequestException("Nie możesz dołączyć do nowego zespołu, dopóki nie opuścisz obecnego.");

        if (team.Members.Count >= team.TeamSize)
            throw new BadRequestException("Zespół osiągnął już swój limit miejsc.");

        student.TeamId = team.Id;

        if (team.Members.Count + 1 == team.TeamSize)
        {
            team.TeamSize = -1;
        }

        var allStudents = team.Members.ToList();
        allStudents.Add(student);

        var topStudent = allStudents
            .OrderByDescending(s => s.GradeAverage ?? 0)
            .ThenBy(s => s.UserId)
            .First();

        team.LeaderId = topStudent.UserId;

        await _context.SaveChangesAsync(ct);
        return true;
    }
}