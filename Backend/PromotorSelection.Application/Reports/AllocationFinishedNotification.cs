using MediatR;
using Microsoft.EntityFrameworkCore;
using PromotorSelection.Application.Common.Interfaces;

namespace PromotorSelection.Application.Allocations;

public record AllocationFinishedNotification : INotification;

public class SendAllocationEmailsHandler : INotificationHandler<AllocationFinishedNotification>
{
    private readonly IApplicationDbContext _context;
    private readonly IEmailService _emailService;

    public SendAllocationEmailsHandler(IApplicationDbContext context, IEmailService emailService)
    {
        _context = context;
        _emailService = emailService;
    }

    public async Task Handle(AllocationFinishedNotification notification, CancellationToken ct)
    {
        var students = await _context.Students
            .Include(s => s.User)
            .Select(s => new
            {
                Email = s.User.Email,
                Name = s.User.FirstName,
                Assignment = _context.Assignments
                    .Where(a => a.StudentId == s.UserId)
                    .Include(a => a.Promotor).ThenInclude(p => p.User)
                    .FirstOrDefault()
            })
            .ToListAsync(ct);

        foreach (var item in students)
        {
            string subject, body;

            if (item.Assignment != null)
            {
                var p = item.Assignment.Promotor.User;
                subject = "Przydział promotora zakończony";
                body = $"Witaj {item.Name},\n\nZostałeś przypisany do promotora: {p.FirstName} {p.LastName}.";
            }
            else
            {
                subject = "Brak przydziału promotora";
                body = $"Witaj {item.Name},\n\nNiestety nie udało się przydzielić Ci promotora w tej turze.";
            }

            await _emailService.SendEmailAsync(item.Email, subject, body);
        }
    }
}