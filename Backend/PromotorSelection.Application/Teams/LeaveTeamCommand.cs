using MediatR;
using Microsoft.EntityFrameworkCore;
using PromotorSelection.Application.Common.Interfaces;

namespace PromotorSelection.Application.Teams;

public record LeaveTeamCommand() : IRequest<bool>;

public class LeaveTeamHandler : IRequestHandler<LeaveTeamCommand, bool>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public LeaveTeamHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<bool> Handle(LeaveTeamCommand request, CancellationToken ct)
    {
        var userId = _currentUser.UserId;
        var student = await _context.Students
            .Include(s => s.Team)
            .ThenInclude(t => t.Members)
            .FirstOrDefaultAsync(s => s.UserId == userId, ct);

        if (student == null || student.TeamId == null) return false;

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

            var newLeader = team.Members.Where(s => s.UserId != userId).OrderByDescending(s => s.GradeAverage ?? 0).First();
            team.LeaderId = newLeader.UserId;
        }

        await _context.SaveChangesAsync(ct);
        return true;
    }
}