using MediatR;
using Microsoft.EntityFrameworkCore;
using PromotorSelection.Application.Common.Interfaces;
using PromotorSelection.Application.Common.Exceptions;
using PromotorSelection.Domain.Entities;

namespace PromotorSelection.Application.Preferences;

public record SetPreferencesCommand(List<int> PromotorIds) : IRequest<bool>;

public class SetPreferencesHandler : IRequestHandler<SetPreferencesCommand, bool>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly ISystemStatusService _statusService;

    public SetPreferencesHandler(IApplicationDbContext context, ICurrentUserService currentUser, ISystemStatusService statusService)
    {
        _context = context;
        _currentUser = currentUser;
        _statusService = statusService;
    }

    public async Task<bool> Handle(SetPreferencesCommand request, CancellationToken ct)
    {
        if (!await _statusService.IsSystemActiveAsync(ct))
            throw new BadRequestException("Modyfikacja danych jest możliwa tylko w wyznaczonym terminie.");

        var currentUserId = _currentUser.UserId ?? throw new BadRequestException("Brak identyfikatora użytkownika w sesji.");

        var student = await _context.Students
            .Include(s => s.Team)
            .ThenInclude(t => t.Members)
            .FirstOrDefaultAsync(s => s.UserId == currentUserId, ct);

        if (student == null)
            throw new NotFoundException("Nie znaleziono profilu studenta w systemie.");

        if (student.TeamId != null && student.Team!.LeaderId != currentUserId)
            throw new BadRequestException("Tylko lider zespołu posiada uprawnienia do dokonania wyboru promotorów.");

        if (request.PromotorIds.Distinct().Count() != request.PromotorIds.Count)
            throw new BadRequestException("Lista wybranych promotorów zawiera powtórzenia.");

        var validPromotorsCount = await _context.Promotors
            .CountAsync(p => request.PromotorIds.Contains(p.UserId), ct);

        if (validPromotorsCount != request.PromotorIds.Count)
            throw new BadRequestException("Jeden lub więcej wybranych promotorów nie istnieje w bazie danych.");

        var studentIdsToUpdate = student.TeamId == null
            ? new List<int> { student.UserId }
            : student.Team.Members.Select(m => m.UserId).ToList();

        var alreadyAssigned = await _context.Assignments
            .AnyAsync(a => a.StudentId != null && studentIdsToUpdate.Contains(a.StudentId.Value), ct);

        if (alreadyAssigned)
            throw new BadRequestException("Nie można zmienić preferencji, ponieważ przydział do promotora został już sfinalizowany.");

        var existingPrefs = await _context.Preferences
            .Where(p => studentIdsToUpdate.Contains(p.StudentId))
            .ToListAsync(ct);

        _context.Preferences.RemoveRange(existingPrefs);

        foreach (var sId in studentIdsToUpdate)
        {
            for (int i = 0; i < request.PromotorIds.Count; i++)
            {
                _context.Preferences.Add(new Preference
                {
                    StudentId = sId,
                    PromotorId = request.PromotorIds[i],
                    Priority = i + 1
                });
            }
        }

        await _context.SaveChangesAsync(ct);
        return true;
    }
}