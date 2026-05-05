using MediatR;
using Microsoft.EntityFrameworkCore;
using PromotorSelection.Application.Common.Interfaces;

namespace PromotorSelection.Application.Users;

public record DeleteUserCommand(int Id) : IRequest;

public class DeleteUserHandler : IRequestHandler<DeleteUserCommand>
{
    private readonly IApplicationDbContext _context;

    public DeleteUserHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task Handle(DeleteUserCommand request, CancellationToken ct)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == request.Id, ct);

        if (user == null) return;

        await _context.BeginTransactionAsync(ct);

        try
        {

            if (user.RoleId == 1) 
            {
                var student = await _context.Students.FirstOrDefaultAsync(s => s.UserId == user.Id, ct);
                if (student != null) _context.Students.Remove(student);
            }
            else if (user.RoleId == 2)
            {
                var promotor = await _context.Promotors.FirstOrDefaultAsync(p => p.UserId == user.Id, ct);
                if (promotor != null) _context.Promotors.Remove(promotor);
            }

            _context.Users.Remove(user);

            await _context.SaveChangesAsync(ct);

            await _context.CommitTransactionAsync(ct);
        }
        catch (Exception)
        {
            await _context.RollbackTransactionAsync(ct);
            throw;
        }
    }
}