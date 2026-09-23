using System.Data;
using System.Text;
using Microsoft.Extensions.Options;
using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;
using ProjectManagerWebAPI.Models.Contas;
using ProjectManagerWebAPI.Models.ShpNot;

namespace ProjectManagerWebAPI.Services;

public class ShpNotException(string message) : Exception(message);

public interface IShpNotRepository
{
    Task<ShpNotEstatisticas> EstatisticasAsync(CancellationToken ct);
    Task<FatiaShpNot> ProcurarAsync(FiltroShpNot filtro, CancellationToken ct);
    Task<ShpNotDetalhe?> ObterAsync(long idt, CancellationToken ct);
}

/// <summary>
/// Leitura dos SHPNOTs recebidos. Os dados são os do WebApiShpNot: o serviço recebe o JSON do
/// Geopost e reparte-o por cerca de 65 tabelas ligadas quase todas uma-a-uma — <b>o JSON
/// original não fica guardado em lado nenhum</b>, nem em CLOB nem em coluna de texto. Por isso
/// este ecrã remonta o envio a partir das tabelas, e por isso cada campo diz de onde vem.
///
/// <para>Só lê. Não há aqui nenhum caminho de escrita: quem grava é o WebApiShpNot, quem
/// marca a FLAGAS400 é o IntegratorAS400.</para>
/// </summary>
public class ShpNotRepository : IShpNotRepository
{
    private readonly string _ligacao;
    private readonly string _esquema;
    private readonly int _timeout;
    private readonly IShpNotCatalogo _catalogo;
    private readonly ILogger<ShpNotRepository> _logger;

    public ShpNotRepository(
        IConfiguration configuracao,
        IOptions<ShpNotOptions> opcoes,
        IShpNotCatalogo catalogo,
        ILogger<ShpNotRepository> logger)
    {
        _catalogo = catalogo;
        _logger = logger;
        _esquema = opcoes.Value.Esquema;
        _timeout = opcoes.Value.TimeoutSegundos;
        _ligacao = configuracao.GetConnectionString(opcoes.Value.ConnectionStringNome)
            ?? throw new InvalidOperationException(
                $"Falta a ligação '{opcoes.Value.ConnectionStringNome}' em ConnectionStrings.");
    }

    private async Task<OracleConnection> AbrirAsync(CancellationToken ct)
    {
        var ligacao = new OracleConnection(_ligacao);
        try
        {
            await ligacao.OpenAsync(ct);
            return ligacao;
        }
        catch (OracleException erro)
        {
            await ligacao.DisposeAsync();
            _logger.LogError(erro, "Falha a ligar à base dos SHPNOT.");
            // A base é intermitente a partir de fora do OKE; dizer que se pode repetir evita
            // que um ORA-12170 passageiro seja lido como avaria da aplicação.
            throw new ShpNotException("Não foi possível ligar à base dos SHPNOT. Tente novamente.");
        }
    }

    private OracleCommand Comando(OracleConnection ligacao, string sql) =>
        new(sql, ligacao) { CommandTimeout = _timeout, BindByName = true };

    // ---------------------------------------------------------------- cartões do topo

    public async Task<ShpNotEstatisticas> EstatisticasAsync(CancellationToken ct)
    {
        // Tudo do dia de hoje, e numa consulta só: uma passagem pela tabela dá os três
        // números. Separá-los por estado custava três passagens para o mesmo trabalho.
        var sql = $"""
            select nvl(FLAGAS400, ' ') flag, count(*) quantos
              from {_esquema}.SHPNOTIN
             where DATAINSERT >= trunc(sysdate)
             group by nvl(FLAGAS400, ' ')
            """;

        await using var ligacao = await AbrirAsync(ct);

        int pendentes = 0, erros = 0, integrados = 0;
        await using (var cmd = Comando(ligacao, sql))
        await using (var leitor = await cmd.ExecuteReaderAsync(ct))
        {
            while (await leitor.ReadAsync(ct))
            {
                var flag = leitor.GetString(0).Trim().ToUpperInvariant();
                var quantos = Convert.ToInt32(leitor.GetValue(1));
                switch (flag)
                {
                    case "Y": integrados += quantos; break;
                    case "E": erros += quantos; break;
                    default: pendentes += quantos; break;
                }
            }
        }

        // O último a entrar, pela chave indexada — max(DATAINSERT) varria a tabela inteira.
        long? ultimoIdt = null;
        DateTime? ultimoRecebido = null;
        await using (var cmd = Comando(ligacao, $"""
            select IDT, DATAINSERT from {_esquema}.SHPNOTIN
             where IDT = (select max(IDT) from {_esquema}.SHPNOTIN)
            """))
        await using (var leitor = await cmd.ExecuteReaderAsync(ct))
        {
            if (await leitor.ReadAsync(ct))
            {
                ultimoIdt = Convert.ToInt64(leitor.GetValue(0));
                ultimoRecebido = Data(leitor.GetValue(1));
            }
        }

        return new ShpNotEstatisticas
        {
            Pendentes = pendentes,
            Erros = erros,
            SucessoHoje = integrados,
            UltimoIdt = ultimoIdt,
            UltimoRecebido = ultimoRecebido,
        };
    }

    // ---------------------------------------------------------------- listagem

    public async Task<FatiaShpNot> ProcurarAsync(FiltroShpNot filtro, CancellationToken ct)
    {
        var onde = new StringBuilder("1 = 1");
        var parametros = new List<OracleParameter>();

        if (filtro.Dia is { } dia)
        {
            // DATAINSERT não tem índice: filtrar por dia varre a tabela. Só se faz quando o
            // ecrã o pede à mão — por omissão a listagem é a dos últimos a entrar, que sai
            // pelo índice do IDT.
            onde.Append(" and shp.DATAINSERT >= :inicio and shp.DATAINSERT < :fim");
            parametros.Add(new OracleParameter("inicio", OracleDbType.Date, dia.Date, ParameterDirection.Input));
            parametros.Add(new OracleParameter("fim", OracleDbType.Date, dia.Date.AddDays(1), ParameterDirection.Input));
        }

        var numero = (filtro.MpsId ?? filtro.Volume)?.Trim();

        if (!string.IsNullOrWhiteSpace(numero))
        {
            // Um número só, duas leituras possíveis: pode ser a guia-mãe (o MPS ID do envio)
            // ou a etiqueta de um dos volumes. Devolve-se sempre o envio inteiro — quem tem
            // na mão um volume quer ver o conjunto a que ele pertence.
            //
            // As duas procuras fazem-se <b>em separado</b>, e não com um OR na mesma consulta:
            // cada uma sozinha custa segundos, mas juntas num OR o Oracle desiste dos índices
            // e passa do minuto. Resolve-se primeiro que envios são, depois lêem-se por chave.
            var ids = await ResolverAsync(numero, ct);
            if (ids.Count == 0)
                return new FatiaShpNot { PaginaAtual = filtro.Pagina, Tamanho = filtro.Tamanho };

            var nomes = new List<string>();
            for (var i = 0; i < ids.Count; i++)
            {
                nomes.Add($":id{i}");
                parametros.Add(new OracleParameter($"id{i}", OracleDbType.Raw, ids[i], ParameterDirection.Input));
            }

            onde.Append($" and shp.ID in ({string.Join(", ", nomes)})");
        }

        if (!string.IsNullOrWhiteSpace(filtro.RespServ))
        {
            onde.Append(" and upper(shp.RESPSERV) = :respserv");
            parametros.Add(new OracleParameter("respserv", OracleDbType.NVarchar2,
                filtro.RespServ.Trim().ToUpperInvariant(), ParameterDirection.Input));
        }

        switch (filtro.Estado?.ToLowerInvariant())
        {
            case "enviado": onde.Append(" and shp.FLAGAS400 = 'Y'"); break;
            case "erro": onde.Append(" and shp.FLAGAS400 = 'E'"); break;
            case "pendente": onde.Append(" and (shp.FLAGAS400 is null or shp.FLAGAS400 not in ('Y','E'))"); break;
        }

        var tamanho = Math.Clamp(filtro.Tamanho, 1, 100);
        var pagina = Math.Max(filtro.Pagina, 1);
        var primeiro = ((pagina - 1) * tamanho) + 1;
        // Pede-se uma linha a mais do que se mostra: é o que diz se há página seguinte sem
        // contar o resto.
        var ultimo = (pagina * tamanho) + 1;

        // A página sai ordenada pela data e hora de receção, que é a coluna que o ecrã mostra.
        // Dentro do corte a ordem continua a ser o IDT: é a única indexada, cresce com cada
        // SHPNOT que entra e por isso dá exactamente a mesma sequência — ordenar milhões de
        // linhas por DATAINSERT, que não tem índice, é que seria impagável. A ordenação por
        // data faz-se no fim, já só sobre as linhas da página.
        //
        // A ordem das operações é o que faz a diferença entre segundos e minutos. Primeiro
        // filtra-se, ordena-se e corta-se <b>só na SHPNOTIN</b>; as cinco juntas do remetente
        // e do destinatário vêm depois, já sobre as dez linhas da página. Com as juntas lá
        // dentro, a mesma consulta demorava três minutos e meio num dia de trabalho.
        var sql = $"""
            select shp.IDT, shp.ID, shp.RESPSERV, shp.FLAGAS400, shp.DATAINSERT, shp.DATAAS400,
                   shp.MPSCOUNT, shipi.MPSID,
                   sadd.NAME sremetente, sadd.COMPNAME sempresa,
                   radd.NAME rdestinatario, radd.COMPNAME rempresa, radd.COUNTRYCODE rpais
              from (
                select * from (
                  select a.*, rownum rn from (
                    select IDT, ID, RESPSERV, FLAGAS400, DATAINSERT, DATAAS400, MPSCOUNT,
                           SHIPMENTINFOSID, SENDERID, RECEIVERID
                      from {_esquema}.SHPNOTIN shp
                     where {onde}
                     order by IDT desc
                  ) a where rownum <= :ultimo
                ) where rn >= :primeiro
              ) shp
              left join {_esquema}.SHIPMENTINFOS shipi on shipi.ID = shp.SHIPMENTINFOSID
              left join {_esquema}.SENDER sender on sender.ID = shp.SENDERID
              left join {_esquema}.SENDERADDRESS sadd on sadd.ID = sender.SENDERADDRESSID
              left join {_esquema}.RECEIVER rec on rec.ID = shp.RECEIVERID
              left join {_esquema}.RECEIVERADDRESS radd on radd.ID = rec.RECEIVERADDRESSID
             order by shp.DATAINSERT desc, shp.IDT desc
            """;

        await using var ligacao = await AbrirAsync(ct);
        await using var cmd = Comando(ligacao, sql);
        foreach (var p in parametros) cmd.Parameters.Add(p);
        cmd.Parameters.Add("ultimo", OracleDbType.Int32, ultimo, ParameterDirection.Input);
        cmd.Parameters.Add("primeiro", OracleDbType.Int32, primeiro, ParameterDirection.Input);

        var itens = new List<ShpNotResumo>();
        await using var leitor = await cmd.ExecuteReaderAsync(ct);
        while (await leitor.ReadAsync(ct))
        {
            var flag = Texto(leitor["FLAGAS400"]);
            itens.Add(new ShpNotResumo
            {
                Idt = Convert.ToInt64(leitor["IDT"]),
                Id = Guid(leitor["ID"]),
                MpsId = Texto(leitor["MPSID"]),
                Remetente = Texto(leitor["sempresa"]) ?? Texto(leitor["sremetente"]),
                Destinatario = Texto(leitor["rempresa"]) ?? Texto(leitor["rdestinatario"]),
                Pais = Texto(leitor["rpais"]),
                Volumes = leitor["MPSCOUNT"] is DBNull ? null : Convert.ToInt32(leitor["MPSCOUNT"]),
                RespServ = Texto(leitor["RESPSERV"]),
                FlagAs400 = flag,
                Estado = Estado(flag),
                Recebido = Data(leitor["DATAINSERT"]),
                ProcessadoAs400 = Data(leitor["DATAAS400"]),
            });
        }

        var haMais = itens.Count > tamanho;
        if (haMais) itens.RemoveAt(itens.Count - 1);

        return new FatiaShpNot
        {
            Itens = itens,
            PaginaAtual = pagina,
            Tamanho = tamanho,
            HaMais = haMais,
        };
    }

    /// <summary>
    /// Que envios correspondem a um número — seja ele a guia ou a etiqueta de um volume.
    /// Duas consultas curtas, cada uma pelo seu índice, em vez de um OR que as junta e as
    /// estraga às duas. O limite existe para o caso de um número repetido em muitos envios
    /// (acontece: o mesmo MPS ID reenviado várias vezes ao longo do dia).
    /// </summary>
    private async Task<List<byte[]>> ResolverAsync(string numero, CancellationToken ct)
    {
        const int Limite = 200;
        var ids = new List<byte[]>();
        var vistos = new HashSet<string>();

        await using var ligacao = await AbrirAsync(ct);

        async Task LerAsync(string sql)
        {
            await using var cmd = Comando(ligacao, sql);
            cmd.Parameters.Add("numero", OracleDbType.NVarchar2, numero, ParameterDirection.Input);
            cmd.Parameters.Add("limite", OracleDbType.Int32, Limite, ParameterDirection.Input);

            await using var leitor = await cmd.ExecuteReaderAsync(ct);
            while (await leitor.ReadAsync(ct))
            {
                if (leitor.GetValue(0) is not byte[] id) continue;
                if (vistos.Add(Convert.ToHexString(id))) ids.Add(id);
            }
        }

        // Pela guia: o MPS ID vive na SHIPMENTINFOS, e a SHPNOTIN aponta-lhe a chave.
        await LerAsync($"""
            select * from (
              select ID from {_esquema}.SHPNOTIN
               where SHIPMENTINFOSID in (select ID from {_esquema}.SHIPMENTINFOS where MPSID = :numero)
               order by IDT desc
            ) where rownum <= :limite
            """);

        // Pela etiqueta de um volume: sobe-se do volume ao envio.
        await LerAsync($"""
            select * from (
              select p.SHPNOTID from {_esquema}.PARCEL p
                join {_esquema}.PARCELINFOS pi on pi.ID = p.PARCELINFOSID
               where pi.PARCELNUMBER = :numero
            ) where rownum <= :limite
            """);

        return ids;
    }

    private static string Estado(string? flag) => flag?.Trim().ToUpperInvariant() switch
    {
        "Y" => "enviado",
        "E" => "erro",
        _ => "pendente",
    };

    // ---------------------------------------------------------------- detalhe

    public async Task<ShpNotDetalhe?> ObterAsync(long idt, CancellationToken ct)
    {
        await using var ligacao = await AbrirAsync(ct);

        var raiz = await LinhaAsync(ligacao, "SHPNOTIN", "IDT", idt, OracleDbType.Int64, ct);
        if (raiz is null) return null;

        var id = raiz["ID"];
        var abas = new List<AbaShpNot>();

        foreach (var aba in ShpNotEstrutura.Abas)
        {
            var blocos = new List<NoShpNot>();

            // O cabeçalho do envio são os campos do próprio SHPNOTIN, que não vêm de nenhuma
            // junta: já estão lidos na raiz.
            if (aba.Chave == "envio")
                blocos.Add(new NoShpNot
                {
                    Titulo = "Cabeçalho",
                    Tabela = "SHPNOTIN",
                    Campos = Campos("SHPNOTIN", raiz),
                });

            foreach (var bloco in aba.Blocos)
                blocos.Add(await MontarAsync(ligacao, bloco, raiz, ct));

            abas.Add(new AbaShpNot { Chave = aba.Chave, Titulo = aba.Titulo, Blocos = blocos });
        }

        var resumo = new ShpNotResumo
        {
            Idt = idt,
            Id = Guid(id),
            MpsId = await ValorAsync(ligacao, "SHIPMENTINFOS", raiz["SHIPMENTINFOSID"], "MPSID", ct),
            RespServ = Texto(raiz["RESPSERV"]),
            FlagAs400 = Texto(raiz["FLAGAS400"]),
            Estado = Estado(Texto(raiz["FLAGAS400"])),
            Volumes = raiz["MPSCOUNT"] is DBNull ? null : Convert.ToInt32(raiz["MPSCOUNT"]),
            Recebido = Data(raiz["DATAINSERT"]),
            ProcessadoAs400 = Data(raiz["DATAAS400"]),
        };

        return new ShpNotDetalhe { Resumo = resumo, Abas = abas };
    }

    /// <summary>Lê um bloco e, recursivamente, os que lhe estão por baixo.</summary>
    private async Task<NoShpNot> MontarAsync(
        OracleConnection ligacao,
        ShpNotEstrutura.Bloco bloco,
        Dictionary<string, object> pai,
        CancellationToken ct)
    {
        if (bloco.Ligacao == ShpNotEstrutura.Ligacao.PaiAponta)
        {
            var chave = pai.GetValueOrDefault(bloco.Chave);
            var linha = chave is null or DBNull
                ? null
                : await LinhaAsync(ligacao, bloco.Tabela, "ID", chave, OracleDbType.Raw, ct);

            if (linha is null)
                return new NoShpNot { Titulo = bloco.Titulo, Tabela = bloco.Tabela, Vazio = true };

            var filhos = new List<NoShpNot>();
            foreach (var filho in bloco.Filhos ?? [])
                filhos.Add(await MontarAsync(ligacao, filho, linha, ct));

            return new NoShpNot
            {
                Titulo = bloco.Titulo,
                Tabela = bloco.Tabela,
                Campos = Campos(bloco.Tabela, linha),
                Filhos = filhos,
            };
        }

        // Coleção: as linhas apontam para o pai.
        var linhas = await LinhasAsync(ligacao, bloco.Tabela, bloco.Chave, pai["ID"], bloco.Ordem, ct);
        var resultado = new List<LinhaShpNot>();

        foreach (var linha in linhas)
        {
            var filhos = new List<NoShpNot>();
            foreach (var filho in bloco.Filhos ?? [])
                filhos.Add(await MontarAsync(ligacao, filho, linha, ct));

            resultado.Add(new LinhaShpNot
            {
                Id = Guid(linha["ID"]),
                Rotulo = bloco.Rotulo is null ? null : Texto(linha.GetValueOrDefault(bloco.Rotulo)),
                Campos = Campos(bloco.Tabela, linha),
                Filhos = filhos,
            });
        }

        return new NoShpNot
        {
            Titulo = bloco.Titulo,
            Tabela = bloco.Tabela,
            Colecao = true,
            Linhas = resultado,
            Vazio = resultado.Count == 0,
        };
    }

    private List<CampoShpNot> Campos(string tabela, Dictionary<string, object> linha) =>
    [
        .. linha
            .Where(c => !ShpNotEstrutura.Escondida(tabela, c.Key))
            .Select(c => _catalogo.Descrever(tabela, c.Key, Texto(c.Value)))
    ];

    // ---------------------------------------------------------------- acesso cru

    private async Task<Dictionary<string, object>?> LinhaAsync(
        OracleConnection ligacao, string tabela, string coluna, object valor,
        OracleDbType tipo, CancellationToken ct)
    {
        await using var cmd = Comando(ligacao, $"select * from {_esquema}.{tabela} where {coluna} = :chave");
        cmd.Parameters.Add("chave", tipo, valor, ParameterDirection.Input);
        await using var leitor = await cmd.ExecuteReaderAsync(ct);
        return await leitor.ReadAsync(ct) ? Ler(leitor) : null;
    }

    private async Task<List<Dictionary<string, object>>> LinhasAsync(
        OracleConnection ligacao, string tabela, string coluna, object valor,
        string? ordem, CancellationToken ct)
    {
        // A ordem vem da estrutura do ecrã (ShpNotEstrutura), nunca do pedido: é uma expressão
        // escrita no código e não texto que chegue de fora.
        var sql = $"select * from {_esquema}.{tabela} where {coluna} = :chave"
                  + (ordem is null ? "" : $" order by {ordem}");

        await using var cmd = Comando(ligacao, sql);
        cmd.Parameters.Add("chave", OracleDbType.Raw, valor, ParameterDirection.Input);

        var linhas = new List<Dictionary<string, object>>();
        await using var leitor = await cmd.ExecuteReaderAsync(ct);
        while (await leitor.ReadAsync(ct)) linhas.Add(Ler(leitor));
        return linhas;
    }

    private async Task<string?> ValorAsync(
        OracleConnection ligacao, string tabela, object? id, string coluna, CancellationToken ct)
    {
        if (id is null or DBNull) return null;
        await using var cmd = Comando(ligacao, $"select {coluna} from {_esquema}.{tabela} where ID = :chave");
        cmd.Parameters.Add("chave", OracleDbType.Raw, id, ParameterDirection.Input);
        return Texto(await cmd.ExecuteScalarAsync(ct));
    }

    private static Dictionary<string, object> Ler(OracleDataReader leitor)
    {
        var linha = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < leitor.FieldCount; i++)
            linha[leitor.GetName(i).ToUpperInvariant()] = leitor.GetValue(i);
        return linha;
    }

    // ---------------------------------------------------------------- conversões

    /// <summary>
    /// Texto de uma célula. As colunas do modelo são NVARCHAR2, mas há RAW (as chaves),
    /// NUMBER e TIMESTAMP — e os booleanos do contrato estão guardados como NUMBER(1), que
    /// aqui se lêem como "0"/"1" e é o próprio ecrã que os traduz.
    /// </summary>
    private static string? Texto(object? valor) => valor switch
    {
        null or DBNull => null,
        byte[] bruto => Convert.ToHexString(bruto),
        OracleTimeStamp t => t.Value.ToString("yyyy-MM-dd HH:mm:ss"),
        DateTime d => d.ToString("yyyy-MM-dd HH:mm:ss"),
        _ => valor.ToString() is { } s && s.TrimEnd().Length > 0 ? s.TrimEnd() : null,
    };

    /// <summary>
    /// O ID em hexadecimal, como a base o mostra. Não se converte para <c>Guid</c> de
    /// propósito: o .NET reordena os primeiros bytes e o identificador deixava de casar com
    /// o que se vê numa consulta à tabela.
    /// </summary>
    private static string Guid(object? valor) =>
        valor is byte[] bruto ? Convert.ToHexString(bruto) : valor?.ToString() ?? "";

    /// <summary>A mesma conversão de datas, para o repositório dos SHPNOTs enviados.</summary>
    public static DateTime? DataPublica(object? valor) => Data(valor);

    private static DateTime? Data(object? valor) => valor switch
    {
        null or DBNull => null,
        DateTime d => d,
        OracleTimeStamp t => t.Value,
        _ => null,
    };
}

/// <summary>Configuração do módulo ShpNot.</summary>
public sealed class ShpNotOptions
{
    public const string Seccao = "ShpNot";

    /// <summary>Nome da ligação em ConnectionStrings.</summary>
    public string ConnectionStringNome { get; set; } = "ShpNot";

    /// <summary>Esquema onde estão as tabelas dos SHPNOT recebidos.</summary>
    public string Esquema { get; set; } = "webapishpnotprd";

    public int TimeoutSegundos { get; set; } = 60;
}
