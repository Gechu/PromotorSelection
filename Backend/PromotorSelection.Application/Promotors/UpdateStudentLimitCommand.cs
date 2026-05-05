using MediatR;
using Microsoft.EntityFrameworkCore;
using PromotorSelection.Application.Common.Interfaces;

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
        var userId = _currentUser.UserId ?? throw new Exception("Brak autoryzacji.");

        var promotor = await _context.Promotors.FirstOrDefaultAsync(p => p.UserId == userId, ct);

        if (promotor == null)
            throw new Exception("Nie znaleziono profilu promotora.");

        if (request.NewLimit < 0)
            throw new Exception("Limit nie może być ujemny.");

        promotor.StudentLimit = request.NewLimit;

        await _context.SaveChangesAsync(ct);
        return true;
    }
}