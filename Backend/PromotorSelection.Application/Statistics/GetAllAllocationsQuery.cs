using MediatR;
using Microsoft.EntityFrameworkCore;
using PromotorSelection.Application.Common.Interfaces;
using PromotorSelection.Application.Dto;

namespace PromotorSelection.Application.Allocations;

public record GetAllAllocationsQuery : IRequest<IEnumerable<AllocationResultDto>>;

public class GetAllAllocationsHandler : IRequestHandler<GetAllAllocationsQuery, IEnumerable<AllocationResultDto>>
{
    private readonly IApplicationDbContext _context;

    public GetAllAllocationsHandler(IApplicationDbContext context) => _context = context;

    public async Task<IEnumerable<AllocationResultDto>> Handle(GetAllAllocationsQuery request, CancellationToken ct)
    {
        return await _context.Assignments
            .Include(a => a.Student).ThenInclude(s => s.User)
            .Include(a => a.Promotor).ThenInclude(p => p.User)
            .Select(a => new AllocationResultDto(
                a.StudentId.Value,
                a.Student.User.FirstName,
                a.Student.User.LastName,
                a.Student.AlbumNumber,
                a.Student.GradeAverage,
                a.PromotorId,
                a.Promotor.User.FirstName,
                a.Promotor.User.LastName
            ))
            .ToListAsync(ct);
    }
}