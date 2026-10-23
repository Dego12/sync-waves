using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SendGrid;
using SendGrid.Helpers.Mail;
using SyncWaves.Auth.Domain.Abstractions;

namespace SyncWaves.Auth.Infrastructure.Implementations;

public class EmailService : IEmailService
{
    private readonly string _apiKey;
    private readonly string _fromEmail;
    private readonly string _fromName;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _logger = logger;
        _apiKey = configuration["SendGrid:ApiKey"]
                  ?? throw new InvalidOperationException("SendGrid:ApiKey is not configured.");

        _fromEmail = configuration["SendGrid:FromEmail"]
                     ?? throw new InvalidOperationException("SendGrid:FromEmail is not configured.");

        _fromName = configuration["SendGrid:FromName"] ?? "SyncWaves";
    }

    public async Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct = default)
    {
        var options = new SendGridClientOptions
        {
            ApiKey = _apiKey
        };
        options.SetDataResidency("eu");

        var client = new SendGridClient(_apiKey);

        var from = new EmailAddress(_fromEmail, _fromName);
        var toAddress = new EmailAddress(to);

        var msg = MailHelper.CreateSingleEmail(from, toAddress, subject, plainTextContent: null, htmlContent: htmlBody);

        var response = await client.SendEmailAsync(msg, ct);

        if ((int)response.StatusCode >= 200 && (int)response.StatusCode < 300)
        {
            _logger.LogInformation("Email sent to {To} with subject {Subject}", to, subject);
        }
        else
        {
            var body = await response.Body.ReadAsStringAsync(ct);
            _logger.LogError("Failed to send email to {To}. Status: {Status}. Body: {Body}", to, response.StatusCode, body);
        }
    }
}
