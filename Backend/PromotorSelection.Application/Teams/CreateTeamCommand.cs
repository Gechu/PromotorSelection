using MediatR;
using Microsoft.EntityFrameworkCore;
using PromotorSelection.Application.Common.Interfaces;
using PromotorSelection.Domain.Entities;

namespace PromotorSelection.Application.Teams;

public record CreateTeamCommand(int DesiredSize) : IRequest<int>;

public class CreateTeamHandler : IRequestHandler<CreateTeamCommand, int>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly ISystemStatusService _statusService;

    public CreateTeamHandler(IApplicationDbContext context, ICurrentUserService currentUser, ISystemStatusService statusService)
    {
        _context = context;
        _currentUser = currentUser;
        _statusService = statusService;
    }

    public async Task<int> Handle(CreateTeamCommand request, CancellationToken ct)
    {
        if (!await _statusService.IsSystemActiveAsync(ct))
            throw new Exception("Modyfikacja danych jest możliwa tylko w wyznaczonym terminie.");

        var userId = _currentUser.UserId;
        var student = await _context.Students
            .FirstOrDefaultAsync(s => s.UserId == userId, ct);

        if (student == null || student.TeamId != null)
            throw new Exception("Nie możesz utworzyć zespołu.");

        int size = Math.Clamp(request.DesiredSize, 2, 6);

        var team = new Team
        {
            TeamSize = size,
            LeaderId = student.UserId
        };

        _context.Teams.Add(team);
        await _context.SaveChangesAsync(ct);

        student.TeamId = team.Id;
        await _context.SaveChangesAsync(ct);

        return team.Id;
    }
}