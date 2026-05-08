using MediatR;
using Microsoft.EntityFrameworkCore;
using PromotorSelection.Application.Common.Interfaces;
using PromotorSelection.Application.Common.Exceptions;

namespace PromotorSelection.Application.Teams;

public record LeaveTeamCommand() : IRequest<bool>;

public class LeaveTeamHandler : IRequestHandler<LeaveTeamCommand, bool>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly ISystemStatusService _statusService;

    public LeaveTeamHandler(IApplicationDbContext context, ICurrentUserService currentUser, ISystemStatusService statusService)
    {
        _context = context;
        _currentUser = currentUser;
        _statusService = statusService;
    }

    public async Task<bool> Handle(LeaveTeamCommand request, CancellationToken ct)
    {
        if (!await _statusService.IsSystemActiveAsync(ct))
            throw new BadRequestException("Opuszczanie zespołu jest możliwe tylko w wyznaczonym terminie.");

        var userId = _currentUser.UserId ?? throw new BadRequestException("Błąd autoryzacji.");

        var student = await _context.Students
            .Include(s => s.Team)
            .ThenInclude(t => t.Members)
            .FirstOrDefaultAsync(s => s.UserId == userId, ct);

        if (student == null)
            throw new NotFoundException("Nie znaleziono profilu studenta.");

        if (student.TeamId == null)
            throw new BadRequestException("Nie należysz do żadnego zespołu.");

        var team = student.Team;


        student.TeamId = null;

        if (team.Members.Count == 1) 
        {
            _context.Teams.Remove(team);
        }
        else
        {

            if (team.TeamSize == -1)
            {
                team.TeamSize = team.Members.Count;
            }

            var newLeader = team.Members
                .Where(s => s.UserId != userId)
                .OrderByDescending(s => s.GradeAverage ?? 0)
                .ThenBy(s => s.UserId)
                .First();

            team.LeaderId = newLeader.UserId;
        }

        await _context.SaveChangesAsync(ct);
        return true;
    }
}