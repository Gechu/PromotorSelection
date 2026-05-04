using MediatR;
using Microsoft.EntityFrameworkCore;
using PromotorSelection.Application.Common.Interfaces;
using BCrypt.Net;

namespace PromotorSelection.Application.Users;

public record UpdateUserCommand(int UserId,string FirstName,string LastName, string Email, string? Password = null, string? AlbumNumber = null,double? GradeAverage = null,int? StudentLimit = null) : IRequest<bool>;

public class UpdateUserHandler : IRequestHandler<UpdateUserCommand, bool>
{
    private readonly IApplicationDbContext _context;

    public UpdateUserHandler(IApplicationDbContext context) => _context = context;

    public async Task<bool> Handle(UpdateUserCommand request, CancellationToken ct)
    {
        var user = await _context.Users
            .Include(u => u.Student)
            .Include(u => u.Promotor)
            .FirstOrDefaultAsync(u => u.Id == request.UserId, ct);

        if (user == null) return false;

        await _context.BeginTransactionAsync(ct);

        try
        {
            user.FirstName = request.FirstName;
            user.LastName = request.LastName;
            user.Email = request.Email;

            if (!string.IsNullOrWhiteSpace(request.Password))
            {
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
            }

            if (user.RoleId == 1 && user.Student != null)
            {
                user.Student.AlbumNumber = request.AlbumNumber ?? user.Student.AlbumNumber;
                user.Student.GradeAverage = request.GradeAverage ?? user.Student.GradeAverage;
            }
            else if (user.RoleId == 2 && user.Promotor != null)
            {
                user.Promotor.StudentLimit = request.StudentLimit ?? user.Promotor.StudentLimit;
            }

            await _context.SaveChangesAsync(ct);
            await _context.CommitTransactionAsync(ct);
            return true;
        }
        catch (Exception)
        {
            await _context.RollbackTransactionAsync(ct);
            throw;
        }
    }
}