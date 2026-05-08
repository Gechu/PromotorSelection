using MediatR;
using Microsoft.EntityFrameworkCore;
using PromotorSelection.Application.Common.Interfaces;
using PromotorSelection.Application.Common.Exceptions;
using PromotorSelection.Application.Dto;

namespace PromotorSelection.Application.Topics;

public record GetTopicsQuery : IRequest<IEnumerable<TopicDto>>;

public class GetTopicsHandler : IRequestHandler<GetTopicsQuery, IEnumerable<TopicDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetTopicsHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<IEnumerable<TopicDto>> Handle(GetTopicsQuery request, CancellationToken ct)
    {
        var userId = _currentUserService.UserId ?? throw new BadRequestException("Błąd autoryzacji.");

        var topics = await _context.Topics
            .Where(t => t.PromotorId == userId)
            .Select(t => new TopicDto(t.Id, t.Title, t.Description, t.PromotorId))
            .ToListAsync(ct);

        if (topics == null)
            throw new NotFoundException("Nie znaleziono tematów dla Twojego profilu.");

        return topics;
    }
}