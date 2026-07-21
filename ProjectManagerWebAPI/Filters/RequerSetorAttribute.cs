using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using ProjectManagerWebAPI.Services;

namespace ProjectManagerWebAPI.Filters;

/// <summary>
/// Restringe um controlador aos utilizadores de um setor do Project Manager.
/// Usa-se em conjunto com [Authorize]: este filtro trata da autorização, não da autenticação.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class RequerSetorAttribute : Attribute, IAsyncAuthorizationFilter
{
    private readonly string _setor;

    public RequerSetorAttribute(string setor)
    {
        _setor = setor;
    }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var utilizador = context.HttpContext.User;

        if (utilizador?.Identity?.IsAuthenticated != true)
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        // MapInboundClaims está desligado no Program.cs, por isso o claim chega como "sub".
        if (!int.TryParse(utilizador.FindFirst("sub")?.Value, out var userId))
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        var acesso = context.HttpContext.RequestServices.GetRequiredService<ISetorAccessService>();

        bool pertence;
        try
        {
            pertence = await acesso.PertenceAoSetorAsync(userId, _setor);
        }
        catch (Exception ex)
        {
            // Sem isto, uma falha na base de dados sai daqui como 500 cru: o try/catch do
            // controlador não cobre os filtros de autorização, que correm antes dele.
            context.HttpContext.RequestServices
                .GetRequiredService<ILoggerFactory>()
                .CreateLogger<RequerSetorAttribute>()
                .LogError(ex, "Falha ao verificar a pertença ao setor {Setor}.", _setor);

            context.Result = new ObjectResult("Não foi possível verificar as suas permissões.")
            {
                StatusCode = StatusCodes.Status503ServiceUnavailable
            };
            return;
        }

        if (!pertence)
        {
            context.Result = new ObjectResult($"Acesso reservado aos utilizadores do setor {_setor}.")
            {
                StatusCode = StatusCodes.Status403Forbidden
            };
        }
    }
}
