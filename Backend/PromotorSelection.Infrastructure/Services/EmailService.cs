using System;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PromotorSelection.Application.Common.Interfaces;

namespace PromotorSelection.Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly ILogger<EmailService> _logger;
    private readonly IConfiguration _configuration;

    public EmailService(ILogger<EmailService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    public async Task SendEmailAsync(string to, string subject, string body)
    {
        var host = _configuration["Mailpit:Host"] ?? "localhost";
        var port = int.Parse(_configuration["Mailpit:Port"] ?? "1025");

        try
        {
            using var client = new SmtpClient(host, port);

            var mailMessage = new MailMessage
            {
                From = new MailAddress("system@promotor.edu.pl", "System Wyboru Promotorów"),
                Subject = subject,
                Body = body,
                IsBodyHtml = false
            };

            mailMessage.To.Add(to);

            await client.SendMailAsync(mailMessage);

            _logger.LogInformation("E-mail do {to} został pomyślnie przechwycony przez Mailpit.", to);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Wystąpił błąd podczas wysyłania wiadomości do {to} przez Mailpit.", to);
            throw;
        }
    }
}