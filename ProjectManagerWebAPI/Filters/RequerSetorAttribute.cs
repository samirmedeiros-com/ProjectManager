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

        if (!await acesso.PertenceAoSetorAsync(userId, _setor))
        {
            context.Result = new ObjectResult($"Acesso reservado aos utilizadores do setor {_setor}.")
            {
                StatusCode = StatusCodes.Status403Forbidden
            };
        }
    }
}
