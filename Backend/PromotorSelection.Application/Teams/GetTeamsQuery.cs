using MediatR;
using Microsoft.EntityFrameworkCore;
using PromotorSelection.Application.Common.Interfaces;
using PromotorSelection.Application.Dto;

namespace PromotorSelection.Application.Teams;

public record GetTeamsQuery() : IRequest<List<TeamDto>>;

public class GetTeamsHandler : IRequestHandler<GetTeamsQuery, List<TeamDto>>
{
    private readonly IApplicationDbContext _context;

    public GetTeamsHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<TeamDto>> Handle(GetTeamsQuery request, CancellationToken ct)
    {
        return await _context.Teams.Include(t => t.Members).Select(t => new TeamDto
            {
                Id = t.Id,
                TeamSize = t.TeamSize,
                LeaderId = t.LeaderId,
                CurrentMembersCount = t.Members.Count
            }).ToListAsync(ct);
    }
}