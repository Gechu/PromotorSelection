using MediatR;
using Microsoft.EntityFrameworkCore;
using PromotorSelection.Application.Common.Interfaces;
using PromotorSelection.Application.Common.Exceptions;

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

        if (user == null)
            throw new NotFoundException($"Nie znaleziono użytkownika o ID {request.Id}.");

        await _context.BeginTransactionAsync(ct);

        try
        {
            if (user.RoleId == 1)
            {
                var student = await _context.Students.FirstOrDefaultAsync(s => s.UserId == user.Id, ct);
                if (student != null)
                {
                    var hasAssignments = await _context.Assignments.AnyAsync(a => a.StudentId == student.UserId, ct);
                    if (hasAssignments)
                        throw new BadRequestException("Nie można usunąć studenta, który posiada już przypisanie do promotora.");

                    _context.Students.Remove(student);
                }
            }
            else if (user.RoleId == 2)
            {
                var promotor = await _context.Promotors.FirstOrDefaultAsync(p => p.UserId == user.Id, ct);
                if (promotor != null)
                {
                    var hasAssignedStudents = await _context.Assignments.AnyAsync(a => a.PromotorId == promotor.UserId, ct);
                    if (hasAssignedStudents)
                        throw new BadRequestException("Nie można usunąć promotora, do którego są przypisani studenci.");

                    _context.Promotors.Remove(promotor);
                }
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