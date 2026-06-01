using MediatR;
using Microsoft.EntityFrameworkCore;
using PromotorSelection.Application.Common.Exceptions;
using PromotorSelection.Application.Common.Interfaces;
using PromotorSelection.Application.Dto;

namespace PromotorSelection.Application.Preferences;

public record GetMyPreferencesQuery : IRequest<List<PreferenceDto>>;

public class GetMyPreferencesHandler : IRequestHandler<GetMyPreferencesQuery, List<PreferenceDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public GetMyPreferencesHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<List<PreferenceDto>> Handle(GetMyPreferencesQuery request, CancellationToken ct)
    {
        var currentUserId = _currentUser.UserId ?? throw new BadRequestException("Brak identyfikatora użytkownika w sesji.");

        var studentExists = await _context.Students.AnyAsync(s => s.UserId == currentUserId, ct);
        if (!studentExists)
            throw new NotFoundException("Nie znaleziono profilu studenta w systemie.");

        var preferences = await _context.Preferences
    .Where(p => p.StudentId == currentUserId)
    .OrderBy(p => p.Priority)
    .Select(p => new PreferenceDto
    {
        PromotorId = p.PromotorId,
        Priority = p.Priority,
        PromotorFirstName = p.Promotor.User.FirstName,
        PromotorLastName = p.Promotor.User.LastName
    })
    .ToListAsync(ct);

        if (!preferences.Any())
            throw new NotFoundException("Brak zadeklarowanych preferencji promotorów dla Twojego konta/zespołu.");

        return preferences;
    }
}