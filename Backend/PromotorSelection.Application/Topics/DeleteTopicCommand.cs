using MediatR;
using PromotorSelection.Infrastructure;

namespace PromotorSelection.Application.Topics;

public record DeleteTopicCommand(int Id) : IRequest<bool>;

public class DeleteTopicHandler : IRequestHandler<DeleteTopicCommand, bool>
{
    private readonly ApplicationDbContext _context;
    public DeleteTopicHandler(ApplicationDbContext context) => _context = context;

    public async Task<bool> Handle(DeleteTopicCommand request, CancellationToken ct)
    {
        var topic = await _context.Topics.FindAsync(new object[] { request.Id }, ct);
        if (topic == null) return false;

        _context.Topics.Remove(topic);
        await _context.SaveChangesAsync(ct);
        return true;
    }
}