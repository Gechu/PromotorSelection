using MediatR;
using Microsoft.EntityFrameworkCore;
using PromotorSelection.Application.Common.Interfaces;

namespace PromotorSelection.Application.Topics;

public record UpdateTopicCommand(int Id, string Title, string Description) : IRequest<bool>;

public class UpdateTopicHandler : IRequestHandler<UpdateTopicCommand, bool>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ISystemStatusService _statusService;

    public UpdateTopicHandler(IApplicationDbContext context, ICurrentUserService currentUserService, ISystemStatusService statusService)
    {
        _context = context;
        _currentUserService = currentUserService;
        _statusService = statusService;
    }

    public async Task<bool> Handle(UpdateTopicCommand request, CancellationToken ct)
    {
        if (!await _statusService.IsSystemActiveAsync(ct))
            throw new Exception("Modyfikacja danych jest możliwa tylko w wyznaczonym terminie.");

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