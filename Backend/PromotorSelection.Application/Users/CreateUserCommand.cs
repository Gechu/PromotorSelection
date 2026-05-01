using MediatR;
using PromotorSelection.Application.Dto;
using PromotorSelection.Domain.Entities;
using PromotorSelection.Infrastructure.Interfaces;
using PromotorSelection.Infrastructure;
using AutoMapper;
using Microsoft.EntityFrameworkCore.Storage;

namespace PromotorSelection.Application.Users;

public class CreateUserCommand : IRequest<UserDto>
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public int RoleId { get; set; } 

    public string? AlbumNumber { get; set; }  = string.Empty;
    public int? StudentLimit { get; set; } = null;
}

public class CreateUserHandler : IRequestHandler<CreateUserCommand, UserDto>
{
    private readonly IUserRepository _repo;
    private readonly IMapper _mapper;
    private readonly ApplicationDbContext _context;

    public CreateUserHandler(IUserRepository repo, IMapper mapper, ApplicationDbContext context)
    {
        _repo = repo;
        _mapper = mapper;
        _context = context;
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

            await _repo.AddAsync(user);
            await _repo.SaveChangesAsync();

            if (request.RoleId == 1)
            {
                await _repo.AddStudentAsync(new Student
                {
                    UserId = user.Id,
                    AlbumNumber = request.AlbumNumber ?? "000000",
                    GradeAverage = null 
                });
                await _repo.SaveChangesAsync();
            }

            else if (request.RoleId == 2)
            {
                await _repo.AddPromotorAsync(new Promotor
                {
                    UserId = user.Id,
                    StudentLimit = request.StudentLimit ?? 10 
                });
                await _repo.SaveChangesAsync();
            }

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