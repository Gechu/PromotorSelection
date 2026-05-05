using MediatR;
using Microsoft.EntityFrameworkCore;
using PromotorSelection.Application.Common.Interfaces;
using PromotorSelection.Application.Dto;
using PromotorSelection.Domain.Entities;

namespace PromotorSelection.Application.Topics;

public record CreateTopicCommand(string Title, string Description) : IRequest<TopicDto>;

public class CreateTopicHandler : IRequestHandler<CreateTopicCommand, TopicDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ISystemStatusService _statusService;

    public CreateTopicHandler(IApplicationDbContext context, ICurrentUserService currentUserService, ISystemStatusService statusService)
    {
        _context = context;
        _currentUserService = currentUserService;
        _statusService = statusService;
    }

    public async Task<TopicDto> Handle(CreateTopicCommand request, CancellationToken ct)
    {
        if (!await _statusService.IsSystemActiveAsync(ct))
            throw new Exception("Modyfikacja danych jest możliwa tylko w wyznaczonym terminie.");

        var userId = _currentUserService.UserId;

        var promotor = await _context.Promotors.FirstOrDefaultAsync(p => p.UserId == userId, ct);

        if (promotor == null) throw new UnauthorizedAccessException();

        var topic = new Topic
        {
            Title = request.Title,
            Description = request.Description,
            PromotorId = promotor.UserId
        };

        _context.Topics.Add(topic);
        await _context.SaveChangesAsync(ct);

        return new TopicDto(topic.Id, topic.Title, topic.Description, topic.PromotorId);
    }
}