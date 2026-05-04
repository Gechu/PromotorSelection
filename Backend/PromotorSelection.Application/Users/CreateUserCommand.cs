using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using PromotorSelection.Application.Dto;
using PromotorSelection.Domain.Entities;
using PromotorSelection.Application.Common.Interfaces;
using BCrypt.Net;

namespace PromotorSelection.Application.Users;

public record CreateUserCommand(string FirstName, string LastName, string Email, string Password, int RoleId, string? AlbumNumber, int? StudentLimit) : IRequest<UserDto>;

public class CreateUserHandler : IRequestHandler<CreateUserCommand, UserDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public CreateUserHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<UserDto> Handle(CreateUserCommand request, CancellationToken ct)
    {
        await _context.BeginTransactionAsync(ct);

        var hashedPassword = BCrypt.Net.BCrypt.HashPassword(request.Password);

        try
        {
            var user = new User
            {
                FirstName = request.FirstName,
                LastName = request.LastName,
                Email = request.Email,
                PasswordHash = hashedPassword,
                RoleId = request.RoleId
            };

            _context.Users.Add(user);

            await _context.SaveChangesAsync(ct);

            if (request.RoleId == 1)
            {
                var student = new Student
                {
                    UserId = user.Id, 
                    AlbumNumber = request.AlbumNumber ?? "000000",
                    GradeAverage = null
                };
                _context.Students.Add(student);
            }
            else if (request.RoleId == 2)
            {
                var promotor = new Promotor
                {
                    UserId = user.Id, 
                    StudentLimit = request.StudentLimit ?? 10
                };
                _context.Promotors.Add(promotor);
            }

            await _context.SaveChangesAsync(ct);

            await _context.CommitTransactionAsync(ct);

            return _mapper.Map<UserDto>(user);
        }
        catch (Exception)
        {
            await _context.RollbackTransactionAsync(ct);
            throw;
        }
    }
}