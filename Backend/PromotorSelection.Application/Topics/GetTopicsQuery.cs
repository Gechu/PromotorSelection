using MediatR;
using Microsoft.EntityFrameworkCore;
using PromotorSelection.Application.Dto;
using PromotorSelection.Infrastructure;

namespace PromotorSelection.Application.Topics;

public record GetTopicsQuery : IRequest<IEnumerable<TopicDto>>;

public class GetTopicsHandler : IRequestHandler<GetTopicsQuery, IEnumerable<TopicDto>>
{
    private readonly ApplicationDbContext _context;
    public GetTopicsHandler(ApplicationDbContext context) => _context = context;

    public async Task<IEnumerable<TopicDto>> Handle(GetTopicsQuery request, CancellationToken ct)
    {
        return await _context.Topics
            .Select(t => new TopicDto(t.Id, t.Title, t.Description, t.PromotorId))
            .ToListAsync(ct);
    }
}