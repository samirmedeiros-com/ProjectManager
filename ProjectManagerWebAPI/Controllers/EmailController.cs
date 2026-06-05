using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectManagerWebAPI.Models;
using ProjectManagerWebAPI.Services;

namespace ProjectManagerWebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class EmailController : ControllerBase
{
    private readonly IEmailService _emailService;
    private readonly ILogger<EmailController> _logger;

    public EmailController(IEmailService emailService, ILogger<EmailController> logger)
    {
        _emailService = emailService;
        _logger = logger;
    }

    /// <summary>
    /// Enviar um email simples
    /// </summary>
    /// <param name="request">Detalhes do email</param>
    /// <returns>Resposta do envio</returns>
    [HttpPost("send")]
    [ProducesResponseType(typeof(EmailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<EmailResponse>> SendEmail([FromBody] EmailRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(new ErrorResponse { Message = "Dados inválidos", Errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList() });

        _logger.LogInformation($"Requisição de envio de email para {request.To}");

        var response = await _emailService.SendEmailAsync(request);

        if (!response.Success)
            return BadRequest(response);

        return Ok(response);
    }

    /// <summary>
    /// Enviar email em lote
    /// </summary>
    /// <param name="requests">Lista de emails para enviar</param>
    /// <returns>Lista de respostas</returns>
    [HttpPost("send-batch")]
    [ProducesResponseType(typeof(List<EmailResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<List<EmailResponse>>> SendEmailBatch([FromBody] List<EmailRequest> requests)
    {
        if (!requests.Any())
            return BadRequest(new ErrorResponse { Message = "Nenhum email para enviar" });

        _logger.LogInformation($"Requisição de envio em lote de {requests.Count} emails");

        var responses = await _emailService.SendEmailBatchAsync(requests);

        var failedCount = responses.Count(r => !r.Success);
        if (failedCount > 0)
            _logger.LogWarning($"Falha ao enviar {failedCount} de {requests.Count} emails");

        return Ok(responses);
    }

    /// <summary>
    /// Testar configuração SMTP
    /// </summary>
    /// <returns>Status da conexão</returns>
    [HttpPost("test")]
    [ProducesResponseType(typeof(EmailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<EmailResponse>> TestSmtp()
    {
        var userEmail = User.FindFirst("email")?.Value;

        if (string.IsNullOrWhiteSpace(userEmail))
            return Unauthorized(new ErrorResponse { Message = "Usuário não autenticado" });

        var testRequest = new EmailRequest
        {
            To = userEmail,
            Subject = "Teste de Configuração SMTP - Project Manager",
            Body = "Este é um email de teste para validar a configuração SMTP do Project Manager.",
            HtmlBody = @"
                <h2>Teste de Configuração SMTP</h2>
                <p>Este é um email de teste para validar a configuração SMTP do Project Manager.</p>
                <p><strong>Se você recebeu este email, a configuração SMTP está funcionando corretamente!</strong></p>
            "
        };

        _logger.LogInformation($"Teste SMTP iniciado para {userEmail}");

        var response = await _emailService.SendEmailAsync(testRequest);

        return Ok(response);
    }
}

public class ErrorResponse
{
    public string Message { get; set; } = "";
    public List<string>? Errors { get; set; }
}
