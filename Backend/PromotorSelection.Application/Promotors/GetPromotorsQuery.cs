using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using PromotorSelection.Application.Dto;
using PromotorSelection.Infrastructure;

namespace PromotorSelection.Application.Promotors;

public class GetPromotorsQuery : IRequest<IEnumerable<PromotorDto>> { }

public class GetPromotorsHandler : IRequestHandler<GetPromotorsQuery, IEnumerable<PromotorDto>>
{
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetPromotorsHandler(ApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<IEnumerable<PromotorDto>> Handle(GetPromotorsQuery request, CancellationToken ct)
    {
        var promotors = await _context.Promotors.Include(p => p.User).Include(p => p.Topics).ToListAsync(ct);

        return _mapper.Map<IEnumerable<PromotorDto>>(promotors);
    }
}