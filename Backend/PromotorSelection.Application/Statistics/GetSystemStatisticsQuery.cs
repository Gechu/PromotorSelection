using MediatR;
using Microsoft.EntityFrameworkCore;
using PromotorSelection.Application.Common.Interfaces;
using PromotorSelection.Application.Dto;

namespace PromotorSelection.Application.Statistics;

public record GetSystemStatisticsQuery : IRequest<StatisticsDto>;

public class GetSystemStatisticsHandler : IRequestHandler<GetSystemStatisticsQuery, StatisticsDto>
{
    private readonly IApplicationDbContext _context;

    public GetSystemStatisticsHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<StatisticsDto> Handle(GetSystemStatisticsQuery request, CancellationToken ct)
    {
        var totalTeams = await _context.Teams.CountAsync(ct);

        var freelancersCount = await _context.Students
            .Where(s => s.TeamId == null)
            .CountAsync(ct);

        var idleStudentsCount = await _context.Students
            .Where(s => !_context.Preferences.Any(p => p.StudentId == s.UserId))
            .CountAsync(ct);

        var promotorOccupancy = await _context.Promotors
            .Include(p => p.User)
            .Select(p => new PromotorOccupancyDto
            {
                PromotorId = p.UserId,
                FirstName = p.User.FirstName,
                LastName = p.User.LastName,
                StudentLimit = p.StudentLimit,
                InterestedStudentsCount = _context.Preferences
                    .Where(pref => pref.PromotorId == p.UserId) 
                    .Select(pref => pref.StudentId)
                    .Distinct()
                    .Count()
            })
            .ToListAsync(ct);

        return new StatisticsDto(
            totalTeams,
            freelancersCount,
            idleStudentsCount,
            promotorOccupancy
        );
    }
}