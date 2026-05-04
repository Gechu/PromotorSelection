using MediatR;
using Microsoft.EntityFrameworkCore;
using PromotorSelection.Application.Common.Interfaces;

namespace PromotorSelection.Application.Topics;

public record UpdateTopicCommand(int Id, string Title, string Description) : IRequest<bool>;

public class UpdateTopicHandler : IRequestHandler<UpdateTopicCommand, bool>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public UpdateTopicHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<bool> Handle(UpdateTopicCommand request, CancellationToken ct)
    {
        var userId = _currentUserService.UserId;

  
        var topic = await _context.Topics
            .FirstOrDefaultAsync(t => t.Id == request.Id && t.Promotor.UserId == userId, ct);

        if (topic == null) return false;

        topic.Title = request.Title;
        topic.Description = request.Description;

        await _context.SaveChangesAsync(ct);
        return true;
    }
}