using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ProjectManagerWebAPI.Filters;

/// <summary>
/// Restringe um controlador aos tokens emitidos por uma aplicação concreta do portal.
///
/// Todas as aplicações assinam com a mesma chave, o mesmo issuer e a mesma audience, por isso
/// [Authorize] sozinho aceita o token de qualquer uma delas. Este filtro lê o claim "app" e
/// garante que credenciais separadas continuam separadas depois do login.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class RequerAppAttribute(string aplicacao) : Attribute, IAuthorizationFilter
{
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var utilizador = context.HttpContext.User;

        if (utilizador?.Identity?.IsAuthenticated != true)
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        // MapInboundClaims está desligado no Program.cs: os claims chegam com o nome curto.
        if (!string.Equals(utilizador.FindFirst("app")?.Value, aplicacao, StringComparison.Ordinal))
        {
            // 401 e não 403: o problema não é falta de permissão, é estar autenticado na
            // aplicação errada. O 401 leva o interceptor do Angular ao login desta app.
            context.Result = new UnauthorizedResult();
        }
    }
}

/// <summary>
/// Exige, além da aplicação, um dos papéis indicados. Usa-se nos comandos que mudam o cluster,
/// que o papel Leitor não pode executar.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class RequerPapelAttribute(params string[] papeis) : Attribute, IAuthorizationFilter
{
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var papel = context.HttpContext.User?.FindFirst("role")?.Value;

        if (papel is null || !papeis.Contains(papel, StringComparer.Ordinal))
        {
            context.Result = new ObjectResult("Não tem permissão para executar esta operação.")
            {
                StatusCode = StatusCodes.Status403Forbidden
            };
        }
    }
}
