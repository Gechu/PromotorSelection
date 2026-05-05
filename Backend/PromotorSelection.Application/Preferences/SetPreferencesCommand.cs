using MediatR;
using Microsoft.EntityFrameworkCore;
using PromotorSelection.Application.Common.Interfaces;
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
            throw new Exception("Modyfikacja danych jest możliwa tylko w wyznaczonym terminie."); 

        var userId = _currentUser.UserId ?? throw new Exception("Nieautoryzowany dostęp.");

        var student = await _context.Students
            .Include(s => s.Team)
            .ThenInclude(t => t.Members)
            .FirstOrDefaultAsync(s => s.UserId == userId, ct);

        if (student == null)
            throw new Exception("Nie znaleziono profilu studenta.");

        if (student.TeamId != null && student.Team!.LeaderId != userId)
            throw new Exception("Tylko lider zespołu może dokonać wyboru promotorów.");

        if (request.PromotorIds.Count != 3)
            throw new Exception("Musisz wybrać dokładnie trzech promotorów.");

        if (request.PromotorIds.Distinct().Count() != 3)
            throw new Exception("Wybrani promotorzy muszą być różni.");

        var studentIdsToUpdate = student.TeamId == null ? new List<int> { student.Id }: student.Team.Members.Select(m => m.Id).ToList();

        var existingPrefs = await _context.Preferences.Where(p => studentIdsToUpdate.Contains(p.StudentId)).ToListAsync(ct);

        _context.Preferences.RemoveRange(existingPrefs);

        foreach (var sId in studentIdsToUpdate)
        {
            for (int i = 0; i < request.PromotorIds.Count; i++)
            {
                var pref = new Preference
                {
                    StudentId = sId,
                    PromotorId = request.PromotorIds[i],
                    Priority = i + 1 
                };
                _context.Preferences.Add(pref);
            }
        }

        await _context.SaveChangesAsync(ct);
        return true;
    }
}