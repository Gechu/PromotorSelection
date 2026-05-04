using MediatR;
using Microsoft.EntityFrameworkCore;
using PromotorSelection.Application.Common.Interfaces;

namespace PromotorSelection.Application.Topics;

public record DeleteTopicCommand(int Id) : IRequest<bool>;

public class DeleteTopicHandler : IRequestHandler<DeleteTopicCommand, bool>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public DeleteTopicHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<bool> Handle(DeleteTopicCommand request, CancellationToken ct)
    {
        var userId = _currentUserService.UserId;

        var topic = await _context.Topics
            .FirstOrDefaultAsync(t => t.Id == request.Id && t.Promotor.UserId == userId, ct);

        if (topic == null) return false;

        _context.Topics.Remove(topic);
        await _context.SaveChangesAsync(ct);
        return true;
    }
}