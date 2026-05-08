using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using PromotorSelection.Application.Dto;
using PromotorSelection.Application.Common.Interfaces;
using PromotorSelection.Application.Common.Exceptions;

namespace PromotorSelection.Application.Promotors;

public record GetPromotorsQuery() : IRequest<IEnumerable<PromotorDto>>;

public class GetPromotorsHandler : IRequestHandler<GetPromotorsQuery, IEnumerable<PromotorDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetPromotorsHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<IEnumerable<PromotorDto>> Handle(GetPromotorsQuery request, CancellationToken ct)
    {
        var promotors = await _context.Promotors
            .Include(p => p.User)
            .Include(p => p.Topics)
            .ToListAsync(ct);

        if (promotors == null || !promotors.Any())
            throw new NotFoundException("Nie znaleziono żadnych promotorów w bazie danych.");

        return _mapper.Map<IEnumerable<PromotorDto>>(promotors);
    }
}