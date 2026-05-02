using MediatR;
using PromotorSelection.Application.Dto;
using PromotorSelection.Domain.Entities;
using PromotorSelection.Infrastructure;

namespace PromotorSelection.Application.Topics;

public record CreateTopicCommand(string Title, string Description, int PromotorId) : IRequest<TopicDto>;

public class CreateTopicHandler : IRequestHandler<CreateTopicCommand, TopicDto>
{
    private readonly ApplicationDbContext _context;
    public CreateTopicHandler(ApplicationDbContext context) => _context = context;

    public async Task<TopicDto> Handle(CreateTopicCommand request, CancellationToken ct)
    {
        var topic = new Topic
        {
            Title = request.Title,
            Description = request.Description,
            PromotorId = request.PromotorId
        };

        _context.Topics.Add(topic);
        await _context.SaveChangesAsync(ct);

        return new TopicDto(topic.Id, topic.Title, topic.Description, topic.PromotorId);
    }
}