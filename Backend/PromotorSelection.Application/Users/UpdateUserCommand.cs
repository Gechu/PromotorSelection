using MediatR;
using Microsoft.EntityFrameworkCore;
using PromotorSelection.Infrastructure;

namespace PromotorSelection.Application.Users;

public record UpdateUserCommand(int UserId,string FirstName,string LastName, string Email,int RoleId,string? AlbumNumber = null,double? GradeAverage = null,int? StudentLimit = null) : IRequest<bool>;

public class UpdateUserHandler : IRequestHandler<UpdateUserCommand, bool>
{
    private readonly ApplicationDbContext _context;

    public UpdateUserHandler(ApplicationDbContext context) => _context = context;

    public async Task<bool> Handle(UpdateUserCommand request, CancellationToken ct)
    {
        var user = await _context.Users.Include(u => u.Student).Include(u => u.Promotor).FirstOrDefaultAsync(u => u.Id == request.UserId, ct);

        if (user == null) return false;

        user.FirstName = request.FirstName;
        user.LastName = request.LastName;
        user.Email = request.Email;

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
        return true;
    }
}