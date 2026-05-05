using MediatR;
using Microsoft.EntityFrameworkCore;
using PromotorSelection.Application.Common.Interfaces;

namespace PromotorSelection.Application.Topics;

public record DeleteTopicCommand(int Id) : IRequest<bool>;

public class DeleteTopicHandler : IRequestHandler<DeleteTopicCommand, bool>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ISystemStatusService _statusService;

    public DeleteTopicHandler(IApplicationDbContext context, ICurrentUserService currentUserService, ISystemStatusService statusService)
    {
        _context = context;
        _currentUserService = currentUserService;
        _statusService = statusService;
    }

    public async Task<bool> Handle(DeleteTopicCommand request, CancellationToken ct)
    {
        if (!await _statusService.IsSystemActiveAsync(ct))
            throw new Exception("Modyfikacja danych jest możliwa tylko w wyznaczonym terminie.");

        var userId = _currentUserService.UserId;

        var topic = await _context.Topics
            .FirstOrDefaultAsync(t => t.Id == request.Id && t.Promotor.UserId == userId, ct);

        if (topic == null) return false;

        _context.Topics.Remove(topic);
        await _context.SaveChangesAsync(ct);
        return true;
    }
}