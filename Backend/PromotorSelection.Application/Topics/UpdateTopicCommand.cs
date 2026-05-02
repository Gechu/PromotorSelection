using MediatR;
using PromotorSelection.Infrastructure;

namespace PromotorSelection.Application.Topics;

public record UpdateTopicCommand(int Id, string Title, string Description) : IRequest<bool>;

public class UpdateTopicHandler : IRequestHandler<UpdateTopicCommand, bool>
{
    private readonly ApplicationDbContext _context;
    public UpdateTopicHandler(ApplicationDbContext context) => _context = context;

    public async Task<bool> Handle(UpdateTopicCommand request, CancellationToken ct)
    {
        var topic = await _context.Topics.FindAsync(new object[] { request.Id }, ct);
        if (topic == null) return false;

        topic.Title = request.Title;
        topic.Description = request.Description;

        await _context.SaveChangesAsync(ct);
        return true;
    }
}