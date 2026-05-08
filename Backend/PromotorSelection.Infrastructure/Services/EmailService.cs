using PromotorSelection.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace PromotorSelection.Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly ILogger<EmailService> _logger;

    public EmailService(ILogger<EmailService> logger)
    {
        _logger = logger;
    }

    public Task SendEmailAsync(string to, string subject, string body)
    {
        Console.WriteLine("\n" + new string('=', 50));
        Console.WriteLine($"[SYMULACJA E-MAIL]");
        Console.WriteLine($"DO: {to}");
        Console.WriteLine($"TEMAT: {subject}");
        Console.WriteLine($"TREŚĆ: {body}");
        Console.WriteLine(new string('=', 50) + "\n");

        _logger.LogInformation("Wysłano symulowany e-mail do: {to}", to);

        return Task.CompletedTask;
    }
}