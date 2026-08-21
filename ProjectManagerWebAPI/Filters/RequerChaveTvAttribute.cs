using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;
using ProjectManagerWebAPI.Services;

namespace ProjectManagerWebAPI.Filters;

/// <summary>
/// Substitui o JWT no mural de TV: em vez de um utilizador autenticado, exige a chave
/// partilhada em <c>?k=</c> (ou no cabeçalho <c>X-Tv-Key</c>, para quem chame por script).
///
/// Os filtros de autorização correm ANTES do try/catch do controlador — uma exceção aqui
/// sai como 500 cru e não aparece no log da aplicação. Daí o try/catch em toda a volta.
/// </summary>
public class RequerChaveTvAttribute : Attribute, IAsyncAuthorizationFilter
{
    public Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        try
        {
            var opcoes = context.HttpContext.RequestServices
                .GetRequiredService<IOptions<TvDashboardOptions>>().Value;

            // O mural nunca deve ser indexado nem guardado em cache intermédia.
            context.HttpContext.Response.Headers["X-Robots-Tag"] = "noindex, nofollow";
            context.HttpContext.Response.Headers["Cache-Control"] = "no-store";

            if (!opcoes.Ativo)
            {
                context.Result = new NotFoundResult();
                return Task.CompletedTask;
            }

            // Sem chave configurada o mural fica fechado: um segredo vazio nunca é
            // um segredo válido, seria o mesmo que deixar o endereço aberto.
            if (string.IsNullOrWhiteSpace(opcoes.Chave))
            {
                context.Result = new ObjectResult(new { mensagem = "Mural de TV não configurado." })
                {
                    StatusCode = StatusCodes.Status503ServiceUnavailable
                };
                return Task.CompletedTask;
            }

            var pedido = context.HttpContext.Request;
            var apresentada = pedido.Query["k"].ToString();

            if (string.IsNullOrEmpty(apresentada))
                apresentada = pedido.Headers["X-Tv-Key"].ToString();

            if (!ChavesIguais(apresentada, opcoes.Chave))
            {
                context.Result = new UnauthorizedObjectResult(new { mensagem = "Chave de acesso inválida." });
                return Task.CompletedTask;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[TV] Erro no filtro de autorização: {ex}");
            context.Result = new ObjectResult(new { mensagem = "Erro ao validar o acesso ao mural." })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }

        return Task.CompletedTask;
    }

    /// <summary>Comparação de tempo constante, para não deixar o tempo de resposta revelar a chave.</summary>
    private static bool ChavesIguais(string? apresentada, string esperada)
    {
        if (string.IsNullOrEmpty(apresentada)) return false;

        var a = Encoding.UTF8.GetBytes(apresentada);
        var b = Encoding.UTF8.GetBytes(esperada);

        return a.Length == b.Length && CryptographicOperations.FixedTimeEquals(a, b);
    }
}
