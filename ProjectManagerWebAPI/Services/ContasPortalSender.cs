using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using ProjectManagerWebAPI.Models.Contas;

namespace ProjectManagerWebAPI.Services;

public interface IContasPortalSender
{
    Task<ResultadoEnvio> EnviarAsync(PedidoEnvio pedido, string utilizador, CancellationToken ct);
}

/// <summary>
/// Envia contas e subcontas ao portal de clientes.
///
/// Duas coisas ficam iguais ao processo automático de propósito, porque é a API do outro lado
/// que as impõe: o token é pedido por multipart/form-data (não por JSON nem por form comum), e
/// grava-se com PUT — só se o PUT falhar é que se tenta o POST. O portal não tem um "criar ou
/// atualizar"; a existência do registo descobre-se pela resposta.
///
/// O que muda é o ambiente: aqui é escolhido à mão, um envio de cada vez. O processo automático
/// dispara sempre para PRD e para QUA.
/// </summary>
public class ContasPortalSender(
    IHttpClientFactory fabrica,
    IContasRepository repositorio,
    IContasAuditService auditoria,
    IOptions<ContasOptions> opcoes,
    ILogger<ContasPortalSender> logger) : IContasPortalSender
{
    private static readonly int[] StatusOk = [200, 201, 204];

    private static readonly JsonSerializerOptions Json = new()
    {
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false
    };

    public async Task<ResultadoEnvio> EnviarAsync(PedidoEnvio pedido, string utilizador, CancellationToken ct)
    {
        var cronometro = System.Diagnostics.Stopwatch.StartNew();
        var nomeAmbiente = (pedido.Ambiente ?? "").Trim().ToUpperInvariant();
        var ambiente = opcoes.Value.Obter(nomeAmbiente)
            ?? throw new ContasException("Ambiente inválido. Escolha PRD ou QUA.");

        if (string.IsNullOrWhiteSpace(ambiente.AuthEndpoint) || string.IsNullOrWhiteSpace(ambiente.ContasEndpoint))
            throw new ContasException($"O ambiente {nomeAmbiente} não está configurado no servidor.");

        var conta = await repositorio.ObterContaAsync(pedido.Conta.Trim(), ct)
            ?? throw new ContasException($"A conta {pedido.Conta} não existe.");

        using var http = fabrica.CreateClient("ContasPortal");
        var token = await ObterTokenAsync(http, ambiente, nomeAmbiente, ct)
            ?? throw new ContasException($"Não foi possível autenticar no portal de {nomeAmbiente}.");

        var resultado = new ResultadoEnvio { Ambiente = nomeAmbiente };

        var contaOk = await GravarNoPortalAsync(http, ambiente.ContasEndpoint, token,
            AccountDto.DaConta(conta), "Conta", conta.Eeent ?? "", resultado, ct);

        // A flag só muda em produção: um envio para qualidade é um teste, e marcar a conta como
        // enviada faria o processo automático saltá-la no envio real para produção.
        if (nomeAmbiente == "PRD")
            await repositorio.MarcarFlagContaAsync(conta.Eeent!, contaOk ? "Y" : "E", ct);

        var subContas = await repositorio.ObterParaEnvioAsync(conta.Eeent!, pedido.SubConta, ct);

        if (!string.IsNullOrWhiteSpace(pedido.SubConta) && subContas.Count == 0)
            throw new ContasException($"A subconta {pedido.SubConta} não existe na conta {conta.Eeent}.");

        foreach (var sub in subContas)
        {
            var subOk = await GravarNoPortalAsync(http, ambiente.SubContasEndpoint, token,
                SubAccountDto.DaSubConta(sub), "Subconta", $"{sub.Ement}/{sub.Emend}", resultado, ct);

            if (nomeAmbiente == "PRD")
                await repositorio.MarcarFlagSubContaAsync(sub.Ement!, sub.Emend!, subOk ? "Y" : "E", ct);
        }

        resultado.Sucesso = resultado.Linhas.All(l => l.Sucesso);
        var falhas = resultado.Linhas.Count(l => !l.Sucesso);
        resultado.Mensagem = resultado.Sucesso
            ? $"Enviado para {nomeAmbiente}: {resultado.Linhas.Count} registo(s) aceites."
            : $"Envio para {nomeAmbiente} com {falhas} falha(s) em {resultado.Linhas.Count} registo(s).";

        cronometro.Stop();
        // O registo é o último passo e não põe em causa o envio: se falhar, o envio já
        // aconteceu e a exceção fica no log da aplicação, não sobe ao utilizador.
        await auditoria.RegistarEnvioAsync(utilizador, pedido, resultado, cronometro.ElapsedMilliseconds);

        return resultado;
    }

    /// <summary>
    /// Pede o token OAuth. As credenciais vão como campos de um formulário multipart — é o
    /// formato que o portal aceita; com form-urlencoded a resposta é 401.
    /// </summary>
    private async Task<string?> ObterTokenAsync(HttpClient http, ContasAmbiente ambiente, string nome, CancellationToken ct)
    {
        try
        {
            using var corpo = new MultipartFormDataContent
            {
                { new StringContent(ambiente.ClientId), "client_id" },
                { new StringContent(ambiente.ClientSecret), "client_secret" },
                { new StringContent(ambiente.GrantType), "grant_type" },
                { new StringContent(ambiente.Utilizador), "username" },
                { new StringContent(ambiente.Password), "password" },
            };

            using var resposta = await http.PostAsync(ambiente.AuthEndpoint, corpo, ct);
            var texto = await resposta.Content.ReadAsStringAsync(ct);

            if (!resposta.IsSuccessStatusCode)
            {
                logger.LogWarning("Autenticação no portal {Ambiente} devolveu {Status}: {Corpo}",
                    nome, (int)resposta.StatusCode, texto);
                return null;
            }

            var token = JsonSerializer.Deserialize<tokenModel>(texto);
            if (token is null || string.IsNullOrWhiteSpace(token.access_token)) return null;

            return $"{token.token_type} {token.access_token}";
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Falha ao autenticar no portal {Ambiente}", nome);
            return null;
        }
    }

    private async Task<bool> GravarNoPortalAsync(HttpClient http, string endpoint, string token,
        object payload, string tipo, string identificacao, ResultadoEnvio resultado, CancellationToken ct)
    {
        var corpo = JsonSerializer.Serialize(payload, payload.GetType(), Json);

        var linhaPut = await ExecutarAsync(http, HttpMethod.Put, endpoint, token, corpo, tipo, identificacao, ct);
        resultado.Linhas.Add(linhaPut);
        if (linhaPut.Sucesso) return true;

        // O PUT falha quando o registo ainda não existe do lado do portal: aí cria-se com POST.
        var linhaPost = await ExecutarAsync(http, HttpMethod.Post, endpoint, token, corpo, tipo, identificacao, ct);
        resultado.Linhas.Add(linhaPost);
        return linhaPost.Sucesso;
    }

    private async Task<LinhaEnvio> ExecutarAsync(HttpClient http, HttpMethod metodo, string endpoint, string token,
        string corpo, string tipo, string identificacao, CancellationToken ct)
    {
        var linha = new LinhaEnvio { Tipo = tipo, Identificacao = identificacao, Metodo = metodo.Method };

        try
        {
            using var pedido = new HttpRequestMessage(metodo, endpoint)
            {
                Content = new StringContent(corpo, Encoding.UTF8, "application/json")
            };
            pedido.Headers.TryAddWithoutValidation("Authorization", token);
            pedido.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            using var resposta = await http.SendAsync(pedido, ct);
            var texto = await resposta.Content.ReadAsStringAsync(ct);

            linha.StatusCode = (int)resposta.StatusCode;
            linha.Sucesso = StatusOk.Contains(linha.StatusCode);
            // A resposta de erro do portal é o único sítio onde se percebe qual o campo recusado;
            // cortada, porque algumas respostas vêm com uma página inteira de HTML.
            linha.Resposta = texto.Length > 2000 ? texto[..2000] + "…" : texto;

            logger.LogInformation("Envio {Tipo} {Id} {Metodo} → {Status}", tipo, identificacao, metodo.Method, linha.StatusCode);
        }
        catch (Exception ex)
        {
            linha.StatusCode = 0;
            linha.Sucesso = false;
            linha.Resposta = ex.Message;
            logger.LogError(ex, "Falha ao enviar {Tipo} {Id} ({Metodo})", tipo, identificacao, metodo.Method);
        }

        return linha;
    }
}
