using MediatR;
using Microsoft.EntityFrameworkCore;
using PromotorSelection.Infrastructure;

namespace PromotorSelection.Application.Users;

public record DeleteUserCommand(int Id) : IRequest;

public class DeleteUserHandler : IRequestHandler<DeleteUserCommand>
{
    private readonly ApplicationDbContext _context;

    public DeleteUserHandler(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task Handle(DeleteUserCommand request, CancellationToken ct)
    {
        var user = await _context.Users.FindAsync(new object[] { request.Id }, ct);
        if (user == null) return;

        using var transaction = await _context.Database.BeginTransactionAsync(ct);

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
            await transaction.CommitAsync(ct);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(ct);
            throw;
        }
    }
}