using System.Net;
using System.Net.Mail;
using ProjectManagerWebAPI.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ProjectManagerWebAPI.Services;

public class SmtpSettings
{
    public string Host { get; set; } = "";
    public int Port { get; set; } = 587;
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
    public string FromEmail { get; set; } = "";
    public string FromName { get; set; } = "Project Manager";
    public bool EnableSsl { get; set; } = true;
    public int Timeout { get; set; } = 30000;
}

public interface IEmailService
{
    Task<EmailResponse> SendEmailAsync(EmailRequest request);
    Task<EmailResponse> SendEmailAsync(string to, string subject, string body, string? htmlBody = null);
    Task<List<EmailResponse>> SendEmailBatchAsync(List<EmailRequest> requests);
}

public class EmailService : IEmailService
{
    private readonly SmtpSettings _smtpSettings;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IOptions<SmtpSettings> smtpSettings, ILogger<EmailService> logger)
    {
        _smtpSettings = smtpSettings.Value;
        _logger = logger;
    }

    public async Task<EmailResponse> SendEmailAsync(EmailRequest request)
    {
        try
        {
            // Validar entrada
            if (string.IsNullOrWhiteSpace(request.To))
                return new EmailResponse { Success = false, Message = "Endereço de email destinatário é obrigatório" };

            if (string.IsNullOrWhiteSpace(request.Subject))
                return new EmailResponse { Success = false, Message = "Assunto do email é obrigatório" };

            if (string.IsNullOrWhiteSpace(request.Body) && string.IsNullOrWhiteSpace(request.HtmlBody))
                return new EmailResponse { Success = false, Message = "Corpo do email é obrigatório" };

            using (var client = new SmtpClient(_smtpSettings.Host, _smtpSettings.Port))
            {
                // Configurar cliente SMTP
                client.EnableSsl = _smtpSettings.EnableSsl;
                client.Timeout = _smtpSettings.Timeout;
                client.Credentials = new NetworkCredential(_smtpSettings.Username, _smtpSettings.Password);
                client.DeliveryMethod = SmtpDeliveryMethod.Network;

                using (var message = new MailMessage())
                {
                    // Configurar remetente
                    message.From = new MailAddress(_smtpSettings.FromEmail, _smtpSettings.FromName);

                    // Configurar destinatários
                    message.To.Add(request.To);

                    if (request.CcEmails?.Any() == true)
                    {
                        foreach (var cc in request.CcEmails.Where(e => !string.IsNullOrWhiteSpace(e)))
                        {
                            message.CC.Add(cc);
                        }
                    }

                    if (request.BccEmails?.Any() == true)
                    {
                        foreach (var bcc in request.BccEmails.Where(e => !string.IsNullOrWhiteSpace(e)))
                        {
                            message.Bcc.Add(bcc);
                        }
                    }

                    // Configurar conteúdo
                    message.Subject = request.Subject;
                    message.Body = request.Body;
                    message.IsBodyHtml = !string.IsNullOrWhiteSpace(request.HtmlBody);

                    if (!string.IsNullOrWhiteSpace(request.HtmlBody))
                    {
                        var plainTextView = AlternateView.CreateAlternateViewFromString(request.Body, null, "text/plain");
                        var htmlView = AlternateView.CreateAlternateViewFromString(request.HtmlBody, null, "text/html");
                        message.AlternateViews.Add(plainTextView);
                        message.AlternateViews.Add(htmlView);
                        message.Body = request.HtmlBody;
                        message.IsBodyHtml = true;
                    }

                    // Adicionar anexos
                    if (request.Attachments?.Any() == true)
                    {
                        foreach (var attachment in request.Attachments)
                        {
                            if (attachment.FileContent?.Length > 0)
                            {
                                var stream = new MemoryStream(attachment.FileContent);
                                message.Attachments.Add(new Attachment(stream, attachment.FileName, attachment.ContentType));
                            }
                        }
                    }

                    // Enviar email
                    await client.SendMailAsync(message);

                    _logger.LogInformation($"Email enviado com sucesso para {request.To}");

                    return new EmailResponse
                    {
                        Success = true,
                        Message = "Email enviado com sucesso",
                        MessageId = message.Headers?["Message-ID"]
                    };
                }
            }
        }
        catch (SmtpException ex)
        {
            _logger.LogError($"Erro SMTP ao enviar email: {ex.Message}", ex);
            return new EmailResponse
            {
                Success = false,
                Message = $"Erro ao enviar email: {ex.Message}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError($"Erro geral ao enviar email: {ex.Message}", ex);
            return new EmailResponse
            {
                Success = false,
                Message = $"Erro ao enviar email: {ex.Message}"
            };
        }
    }

    public async Task<EmailResponse> SendEmailAsync(string to, string subject, string body, string? htmlBody = null)
    {
        return await SendEmailAsync(new EmailRequest
        {
            To = to,
            Subject = subject,
            Body = body,
            HtmlBody = htmlBody
        });
    }

    public async Task<List<EmailResponse>> SendEmailBatchAsync(List<EmailRequest> requests)
    {
        var responses = new List<EmailResponse>();

        foreach (var request in requests)
        {
            var response = await SendEmailAsync(request);
            responses.Add(response);

            // Pequeno delay entre emails para evitar throttling
            await Task.Delay(100);
        }

        return responses;
    }
}
