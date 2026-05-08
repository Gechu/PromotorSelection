using MediatR;
using Microsoft.EntityFrameworkCore;
using PromotorSelection.Application.Common.Interfaces;
using PromotorSelection.Application.Common.Exceptions;
using BCrypt.Net;

namespace PromotorSelection.Application.Users;

public record UpdateUserCommand(int UserId, string FirstName, string LastName, string Email, string? Password = null, string? AlbumNumber = null, double? GradeAverage = null, int? StudentLimit = null) : IRequest<bool>;

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

        if (user == null)
            throw new NotFoundException($"Nie znaleziono użytkownika o ID {request.UserId}.");


        var emailTaken = await _context.Users.AnyAsync(u => u.Email == request.Email && u.Id != request.UserId, ct);
        if (emailTaken)
            throw new BadRequestException($"Adres email {request.Email} jest już zajęty.");

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
                if (!string.IsNullOrWhiteSpace(request.AlbumNumber))
                {
                    var albumTaken = await _context.Students
                        .AnyAsync(s => s.AlbumNumber == request.AlbumNumber && s.UserId != user.Id, ct);
                    if (albumTaken)
                        throw new BadRequestException($"Numer albumu {request.AlbumNumber} jest już przypisany do innego studenta.");

                    user.Student.AlbumNumber = request.AlbumNumber;
                }
                user.Student.GradeAverage = request.GradeAverage ?? user.Student.GradeAverage;
            }
            else if (user.RoleId == 2 && user.Promotor != null)
            {
                if (request.StudentLimit.HasValue && request.StudentLimit.Value < 0)
                    throw new BadRequestException("Limit studentów nie może być ujemny.");

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