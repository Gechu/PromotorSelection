using AutoMapper;
using MediatR;
using PromotorSelection.Application.Dto;
using PromotorSelection.Domain.Entities;
using PromotorSelection.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

namespace PromotorSelection.Application.Users;

public record CreateUserCommand(string FirstName,string LastName,string Email,string Password,int RoleId,string? AlbumNumber,int? StudentLimit) : IRequest<UserDto>;

public class CreateUserHandler : IRequestHandler<CreateUserCommand, UserDto>
{
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;

    public CreateUserHandler(ApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<UserDto> Handle(CreateUserCommand request, CancellationToken ct)
    {
        using IDbContextTransaction transaction = await _context.Database.BeginTransactionAsync(ct);

        try
        {
            var user = new User
            {
                FirstName = request.FirstName,
                LastName = request.LastName,
                Email = request.Email,
                PasswordHash = request.Password, //todo:hashowanie
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
            await transaction.CommitAsync(ct);

            return _mapper.Map<UserDto>(user);
        }
        catch (Exception)
        {
            await transaction.RollbackAsync(ct);
            throw;
        }
    }
}