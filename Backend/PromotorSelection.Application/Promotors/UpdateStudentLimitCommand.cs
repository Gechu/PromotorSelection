using MediatR;
using Microsoft.EntityFrameworkCore;
using PromotorSelection.Application.Common.Interfaces;
using PromotorSelection.Application.Common.Exceptions;

namespace PromotorSelection.Application.Promotors;

public record UpdateStudentLimitCommand(int NewLimit) : IRequest<bool>;

public class UpdateStudentLimitHandler : IRequestHandler<UpdateStudentLimitCommand, bool>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public UpdateStudentLimitHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<bool> Handle(UpdateStudentLimitCommand request, CancellationToken ct)
    {
        var userId = _currentUser.UserId ?? throw new BadRequestException("Brak autoryzacji lub wygasła sesja użytkownika.");

        var promotor = await _context.Promotors.FirstOrDefaultAsync(p => p.UserId == userId, ct);

        if (promotor == null)
            throw new NotFoundException("Nie znaleziono profilu promotora powiązanego z zalogowanym użytkownikiem.");

        if (request.NewLimit < 0)
            throw new BadRequestException("Limit studentów nie może być wartością ujemną.");

        var currentAssignmentsCount = await _context.Assignments
            .CountAsync(a => a.PromotorId == userId, ct);

        if (request.NewLimit < currentAssignmentsCount)
            throw new BadRequestException($"Nie można zmniejszyć limitu do {request.NewLimit}, ponieważ masz już przypisanych {currentAssignmentsCount} studentów.");

        promotor.StudentLimit = request.NewLimit;

        await _context.SaveChangesAsync(ct);
        return true;
    }
}