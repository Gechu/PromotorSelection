using MediatR;
using PromotorSelection.Application.Dto;
using AutoMapper;
using PromotorSelection.Infrastructure.Interfaces;

namespace PromotorSelection.Application.Users;

public record GetUsersQuery : IRequest<IEnumerable<UserDto>>;

public class GetUsersHandler : IRequestHandler<GetUsersQuery, IEnumerable<UserDto>>
{
    private readonly IUserRepository _repo;
    private readonly IMapper _mapper;
    public GetUsersHandler(IUserRepository repo, IMapper mapper) { _repo = repo; _mapper = mapper; }

    public async Task<IEnumerable<UserDto>> Handle(GetUsersQuery request, CancellationToken ct)
    {
        var users = await _repo.GetAllAsync();
        return _mapper.Map<IEnumerable<UserDto>>(users);
    }
}