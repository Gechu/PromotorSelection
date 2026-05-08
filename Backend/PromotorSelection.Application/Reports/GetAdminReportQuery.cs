using MediatR;
using Microsoft.EntityFrameworkCore;
using PromotorSelection.Application.Common.Interfaces;
using PromotorSelection.Application.Dto;

namespace PromotorSelection.Application.Statistics;

public record GetAdminReportQuery : IRequest<AdminReportDto>;

public class GetAdminReportHandler : IRequestHandler<GetAdminReportQuery, AdminReportDto>
{
    private readonly IApplicationDbContext _context;

    public GetAdminReportHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<AdminReportDto> Handle(GetAdminReportQuery request, CancellationToken ct)
    {
        var promotors = await _context.Promotors
            .Include(p => p.User)
            .ToListAsync(ct);

        var assignments = await _context.Assignments
            .Include(a => a.Student).ThenInclude(s => s.User)
            .Include(a => a.Promotor).ThenInclude(p => p.User)
            .ToListAsync(ct);

        var report = new AdminReportDto
        {
            PromotorSummaries = promotors.Select(p => {
                var assignedToThisPromotor = assignments.Count(a => a.PromotorId == p.UserId);
                return new PromotorSummaryDto
                {
                    Name = $"{p.User.FirstName} {p.User.LastName}",
                    Limit = p.StudentLimit,
                    AssignedCount = assignedToThisPromotor,
                    RemainingSlots = p.StudentLimit - assignedToThisPromotor
                };
            }).ToList(),

            AllAssignments = assignments.Select(a => new StudentReportItem
            {
                StudentName = $"{a.Student.User.FirstName} {a.Student.User.LastName}",
                AlbumNumber = a.Student.AlbumNumber,
                Grade = a.Student.GradeAverage ?? 0,
                AssignedPromotor = $"{a.Promotor.User.FirstName} {a.Promotor.User.LastName}"
            }).ToList()
        };

        return report;
    }
}