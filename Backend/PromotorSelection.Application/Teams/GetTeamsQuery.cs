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
        return await _context.Teams
            .Include(t => t.Members).ThenInclude(s => s.User)
            .Select(t => new TeamDto
            {
                Id = t.Id,
                TeamSize = t.TeamSize,
                LeaderId = t.LeaderId,
                CurrentMembersCount = t.Members.Count,
                Members = t.Members.Select(s => new TeamMemberDto
                {
                    UserId = s.UserId,
                    FirstName = s.User.FirstName,
                    LastName = s.User.LastName
                }).ToList()
            }).ToListAsync(ct);
    }
}