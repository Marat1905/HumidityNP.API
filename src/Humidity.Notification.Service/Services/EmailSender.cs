using Humidity.Notification.Service.Options;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Humidity.Notification.Service.Services;

/// <summary>
/// Отправка email через SMTP. Обёртка над MailKit.
/// </summary>
public class EmailSender
{
    private readonly SmtpOptions _smtpOptions;
    private readonly ILogger<EmailSender> _logger;

    public EmailSender(
        IOptions<SmtpOptions> smtpOptions,
        ILogger<EmailSender> logger)
    {
        _smtpOptions = smtpOptions.Value;
        _logger = logger;
    }

    /// <summary>
    /// Отправить письмо с HTML-телом нескольким получателям.
    /// </summary>
    /// <param name="to">Список email-адресов.</param>
    /// <param name="subject">Тема письма.</param>
    /// <param name="htmlBody">HTML-тело письма.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    public async Task SendAsync(
        IEnumerable<string> to,
        string subject,
        string htmlBody,
        CancellationToken cancellationToken = default)
    {
        var recipients = to.ToList();
        if (recipients.Count == 0)
        {
            _logger.LogWarning("Список получателей пуст, письмо не будет отправлено.");
            return;
        }

        var message = new MimeMessage();
        message.From.Add(MailboxAddress.Parse(_smtpOptions.From));
        foreach (var email in recipients)
        {
            message.To.Add(MailboxAddress.Parse(email));
        }
        message.Subject = subject;

        var builder = new BodyBuilder
        {
            HtmlBody = htmlBody
        };
        message.Body = builder.ToMessageBody();

        using var client = new SmtpClient();

        var secureOption = _smtpOptions.UseSsl
            ? SecureSocketOptions.StartTls
            : SecureSocketOptions.None;

        await client.ConnectAsync(_smtpOptions.Host, _smtpOptions.Port, secureOption, cancellationToken);

        if (!string.IsNullOrWhiteSpace(_smtpOptions.Username))
        {
            await client.AuthenticateAsync(_smtpOptions.Username, _smtpOptions.Password, cancellationToken);
        }

        await client.SendAsync(message, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);

        _logger.LogInformation(
            "Письмо отправлено {Count} получателям: {Recipients}. Тема: {Subject}",
            recipients.Count, string.Join(", ", recipients), subject);
    }
}