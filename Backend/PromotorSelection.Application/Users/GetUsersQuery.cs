using MediatR;
using Microsoft.EntityFrameworkCore;
using PromotorSelection.Application.Dto;
using PromotorSelection.Application.Common.Interfaces;
using AutoMapper;

namespace PromotorSelection.Application.Users;

public record GetUsersQuery : IRequest<IEnumerable<UserDto>>;

public class GetUsersHandler : IRequestHandler<GetUsersQuery, IEnumerable<UserDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetUsersHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<IEnumerable<UserDto>> Handle(GetUsersQuery request, CancellationToken ct)
    {
        var users = await _context.Users.Include(u => u.Student).Include(u => u.Promotor).ToListAsync(ct);

        return _mapper.Map<IEnumerable<UserDto>>(users);
    }
}