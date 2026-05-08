using MediatR;
using Microsoft.EntityFrameworkCore;
using PromotorSelection.Application.Common.Interfaces;
using PromotorSelection.Application.Common.Exceptions;
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
            throw new BadRequestException("Modyfikacja danych zespołowych jest możliwa tylko w wyznaczonym terminie.");

        var userId = _currentUser.UserId ?? throw new BadRequestException("Błąd autoryzacji: Brak identyfikatora użytkownika.");

        var student = await _context.Students
            .FirstOrDefaultAsync(s => s.UserId == userId, ct);

        if (student == null)
            throw new NotFoundException("Nie znaleziono profilu studenta.");

        if (student.TeamId != null)
            throw new BadRequestException("Nie możesz utworzyć nowego zespołu, ponieważ już należysz do innego zespołu.");

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