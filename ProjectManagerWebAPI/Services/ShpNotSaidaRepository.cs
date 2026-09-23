using System.Data;
using System.Text;
using Microsoft.Extensions.Options;
using Oracle.ManagedDataAccess.Client;
using ProjectManagerWebAPI.Models.ShpNot;

namespace ProjectManagerWebAPI.Services;

public interface IShpNotSaidaRepository
{
    Task<FilaSaida> FilaAsync(CancellationToken ct);
    Task<List<ShpNotResumo>> ProcurarAsync(FiltroShpNot filtro, CancellationToken ct);
    Task<ShpNotDetalhe?> ObterAsync(long idt, CancellationToken ct);
}

/// <summary>
/// Os SHPNOTs que <b>enviamos</b> ao Geopost, em <c>GROUPSHPNOT.GEODT01SPN</c> (o envio) e
/// <c>GEODT02SPN</c> (os volumes). São o espelho do que a receção guarda: aqui um envio é uma
/// linha larga de ~355 colunas com nomes de AS400, lá são 65 tabelas ligadas.
///
/// <para><b>As colunas daqui são os aliases da VW_SHPNOT_AS400.</b> É por isso que este ecrã
/// consegue mostrar, em cada campo enviado, o nome que ele tem no JSON e onde o mesmo dado
/// fica guardado quando somos nós a receber — sem um segundo dicionário.</para>
///
/// <para>Só lê. Quem envia e marca as flags é a consola do projeto ShpNot.</para>
/// </summary>
public class ShpNotSaidaRepository : IShpNotSaidaRepository
{
    private const string Esquema = "groupshpnot";
    private const string Envios = $"{Esquema}.GEODT01SPN";
    private const string Volumes = $"{Esquema}.GEODT02SPN";

    /// <summary>
    /// Colunas de controlo: não são dados do envio, são o estado das três filas. Ficam numa
    /// aba própria e fora dos blocos do envio.
    /// </summary>
    private static readonly string[] Controlo =
    [
        "IDT", "FLAGENV", "RESPSERV", "DATAHORAENV",
        "FLAGENV_SCAN", "RESPSERV_SCAN", "DATAHORAENV_SCAN",
        "FLAGENV_DESP", "RESPSERV_DESP", "DATAHORAENV_DESP",
        "TIPOSHP", "DATAHORA_INSERT", "SEQ",
    ];

    private readonly string _ligacao;
    private readonly int _timeout;
    private readonly IShpNotCatalogo _catalogo;
    private readonly ILogger<ShpNotSaidaRepository> _logger;

    public ShpNotSaidaRepository(
        IConfiguration configuracao,
        IOptions<ShpNotOptions> opcoes,
        IShpNotCatalogo catalogo,
        ILogger<ShpNotSaidaRepository> logger)
    {
        _catalogo = catalogo;
        _logger = logger;
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
            _logger.LogError(erro, "Falha a ligar à base dos SHPNOT enviados.");
            throw new ShpNotException("Não foi possível ligar à base dos SHPNOT. Tente novamente.");
        }
    }

    private OracleCommand Comando(OracleConnection ligacao, string sql) =>
        new(sql, ligacao) { CommandTimeout = _timeout, BindByName = true };

    // ---------------------------------------------------------------- fila

    public async Task<FilaSaida> FilaAsync(CancellationToken ct)
    {
        await using var ligacao = await AbrirAsync(ct);

        // Tudo do dia de hoje e numa consulta só. A data tem índice (IDX_GEODT01SPN_TV
        // começa por DATAHORA_INSERT), por isso a janela do dia é uma leitura curta e as
        // três filas saem dela sem custo adicional.
        int pendentes = 0, erros = 0, entregues = 0, scan = 0, despacho = 0;
        await using (var cmd = Comando(ligacao, $"""
            select nvl(FLAGENV, ' ') flag, count(*) quantos,
                   sum(case when FLAGENV_SCAN = 'P' then 1 else 0 end) por_scan,
                   sum(case when FLAGENV_DESP = 'P' then 1 else 0 end) por_despachar
              from {Envios}
             where DATAHORA_INSERT >= trunc(sysdate)
             group by nvl(FLAGENV, ' ')
            """))
        await using (var leitor = await cmd.ExecuteReaderAsync(ct))
        {
            while (await leitor.ReadAsync(ct))
            {
                var quantos = Convert.ToInt32(leitor.GetValue(1));
                scan += Convert.ToInt32(leitor.GetValue(2));
                despacho += Convert.ToInt32(leitor.GetValue(3));

                switch (leitor.GetString(0).Trim().ToUpperInvariant())
                {
                    case "Y": entregues += quantos; break;
                    case "E": erros += quantos; break;
                    default: pendentes += quantos; break;
                }
            }
        }

        DateTime? ultimo = null;
        await using (var cmd = Comando(ligacao, $"""
            select max(DATAHORA_INSERT) from {Envios}
             where DATAHORA_INSERT > sysdate - 2
            """))
        {
            ultimo = ShpNotRepository.DataPublica(await cmd.ExecuteScalarAsync(ct));
        }

        return new FilaSaida
        {
            Pendentes = pendentes,
            Erros = erros,
            SucessoHoje = entregues,
            PendentesScan = scan,
            PendentesDespacho = despacho,
            UltimoInserido = ultimo,
        };
    }

    // ---------------------------------------------------------------- pesquisa

    public async Task<List<ShpNotResumo>> ProcurarAsync(FiltroShpNot filtro, CancellationToken ct)
    {
        var onde = new StringBuilder("1 = 1");
        var parametros = new List<OracleParameter>();

        var numero = (filtro.MpsId ?? filtro.Volume)?.Trim();

        if (!string.IsNullOrWhiteSpace(numero))
        {
            // O mesmo que do lado da receção: o número tanto pode ser a guia-mãe como um dos
            // volumes, e devolve-se sempre a guia — aqui a ligação faz-se por MPSMASTER.
            //
            // Sem trim e sem upper, de propósito: MPSIDX tem índice e qualquer função à volta
            // da coluna deita-o fora. Medido: 2 s assim, 3m40s com trim(MPSIDX).
            // Resolve-se primeiro que guias são, e só depois se lêem: um OR entre a coluna e
            // a subconsulta dos volumes faz o Oracle desistir dos índices — do lado da
            // recepção, a mesma escrita passava do minuto.
            var guias = await GuiasAsync(numero, ct);

            var nomes = new List<string>();
            for (var i = 0; i < guias.Count; i++)
            {
                nomes.Add($":guia{i}");
                parametros.Add(new OracleParameter($"guia{i}", OracleDbType.Varchar2,
                    guias[i], ParameterDirection.Input));
            }

            onde.Append($" and MPSIDX in ({string.Join(", ", nomes)})");
        }

        if (filtro.Dia is { } dia)
        {
            onde.Append(" and DATAHORA_INSERT >= :inicio and DATAHORA_INSERT < :fim");
            parametros.Add(new OracleParameter("inicio", OracleDbType.Date, dia.Date, ParameterDirection.Input));
            parametros.Add(new OracleParameter("fim", OracleDbType.Date, dia.Date.AddDays(1), ParameterDirection.Input));
        }

        switch (filtro.Estado?.ToLowerInvariant())
        {
            case "enviado": onde.Append(" and FLAGENV = 'Y'"); break;
            case "erro": onde.Append(" and FLAGENV = 'E'"); break;
            case "pendente": onde.Append(" and FLAGENV = 'N'"); break;
        }

        var quantos = Math.Clamp(filtro.Tamanho, 1, 100) * Math.Max(filtro.Pagina, 1) + 1;

        var sql = $"""
            select * from (
              select IDT, MPSIDX, MPSCOUNTX, FLAGENV, DATAHORAENV, DATAHORA_INSERT, TIPOSHP,
                     RESPSERV, SNAME1X, SCOMPNAMEX, RNAME1X, RCOMPNAMEX, RCOUNTRYCX
                from {Envios}
               where {onde}
               order by DATAHORA_INSERT desc
            ) where rownum <= :quantos
            """;

        await using var ligacao = await AbrirAsync(ct);
        await using var cmd = Comando(ligacao, sql);
        foreach (var p in parametros) cmd.Parameters.Add(p);
        cmd.Parameters.Add("quantos", OracleDbType.Int32, quantos, ParameterDirection.Input);

        var itens = new List<ShpNotResumo>();
        await using var leitor = await cmd.ExecuteReaderAsync(ct);
        while (await leitor.ReadAsync(ct))
        {
            var flag = Texto(leitor["FLAGENV"]);
            itens.Add(new ShpNotResumo
            {
                Sentido = SentidoShpNot.Saida,
                Idt = Convert.ToInt64(leitor["IDT"]),
                Id = Texto(leitor["MPSIDX"]) ?? "",
                MpsId = Texto(leitor["MPSIDX"]),
                Remetente = Texto(leitor["SCOMPNAMEX"]) ?? Texto(leitor["SNAME1X"]),
                Destinatario = Texto(leitor["RCOMPNAMEX"]) ?? Texto(leitor["RNAME1X"]),
                Pais = Texto(leitor["RCOUNTRYCX"]),
                Volumes = int.TryParse(Texto(leitor["MPSCOUNTX"]), out var v) ? v : null,
                RespServ = Texto(leitor["TIPOSHP"]),
                FlagAs400 = flag,
                Estado = Estado(flag),
                Recebido = ShpNotRepository.DataPublica(leitor["DATAHORA_INSERT"]),
                ProcessadoAs400 = ShpNotRepository.DataPublica(leitor["DATAHORAENV"]),
                // A resposta do serviço só interessa quando correu mal: nos outros casos é
                // o "OK" de sempre e só roubava espaço à linha.
                Erro = Estado(flag) == "erro" ? Texto(leitor["RESPSERV"]) : null,
            });
        }

        return itens;
    }

    /// <summary>
    /// As guias a que um número corresponde: ele próprio, se for uma guia, mais as guias dos
    /// volumes com esse número. A lista nunca é vazia — sem isso o <c>in ()</c> ficava sem
    /// argumentos e a consulta não compilava.
    /// </summary>
    private async Task<List<string>> GuiasAsync(string numero, CancellationToken ct)
    {
        var guias = new List<string> { numero };

        await using var ligacao = await AbrirAsync(ct);
        await using var cmd = Comando(ligacao,
            $"select distinct MPSMASTER from {Volumes} where MPSID = :numero and rownum <= 50");
        cmd.Parameters.Add("numero", OracleDbType.Varchar2, numero, ParameterDirection.Input);

        await using var leitor = await cmd.ExecuteReaderAsync(ct);
        while (await leitor.ReadAsync(ct))
        {
            if (Texto(leitor.GetValue(0)) is { } guia && !guias.Contains(guia)) guias.Add(guia);
        }

        return guias;
    }

    /// <summary>
    /// A letra da FLAGENV lida como estado. Aqui <c>Y</c> quer dizer "entregue ao Geopost",
    /// e não "integrado no AS400" como do lado da receção — é o mesmo ecrã a contar as duas
    /// pontas da mesma viagem.
    /// </summary>
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

        Dictionary<string, object>? linha;
        await using (var cmd = Comando(ligacao, $"select * from {Envios} where IDT = :idt"))
        {
            cmd.Parameters.Add("idt", OracleDbType.Int64, idt, ParameterDirection.Input);
            await using var leitor = await cmd.ExecuteReaderAsync(ct);
            linha = await leitor.ReadAsync(ct) ? Ler(leitor) : null;
        }

        if (linha is null) return null;

        var mpsId = Texto(linha.GetValueOrDefault("MPSIDX"));

        // Os campos do envio arrumam-se pelas abas da receção: o alias de cada coluna diz a
        // que tabela o campo pertence lá, e essa tabela diz a aba.
        var porBloco = new Dictionary<(string Aba, string Bloco), List<CampoShpNot>>();
        var controlo = new List<CampoShpNot>();
        var outros = new List<CampoShpNot>();

        foreach (var (coluna, valor) in linha)
        {
            var campo = _catalogo.DescreverSaida("GEODT01SPN", coluna, Texto(valor));

            if (Controlo.Contains(coluna, StringComparer.OrdinalIgnoreCase))
            {
                controlo.Add(campo);
                continue;
            }

            var tabela = _catalogo.TabelaDoAlias(coluna);
            if (tabela is not null && ShpNotEstrutura.OndeVive.TryGetValue(tabela, out var onde))
            {
                if (!porBloco.TryGetValue(onde, out var campos))
                    porBloco[onde] = campos = [];
                campos.Add(campo);
            }
            else
            {
                outros.Add(campo);
            }
        }

        var abas = new List<AbaShpNot>();

        foreach (var aba in ShpNotEstrutura.Abas)
        {
            var blocos = porBloco
                .Where(p => p.Key.Aba == aba.Chave)
                .Select(p => new NoShpNot
                {
                    Titulo = p.Key.Bloco,
                    Tabela = "GEODT01SPN",
                    Campos = p.Value,
                })
                .ToList();

            if (aba.Chave == "volumes")
                blocos.Add(await VolumesAsync(ligacao, mpsId, ct));

            if (blocos.Count > 0)
                abas.Add(new AbaShpNot { Chave = aba.Chave, Titulo = aba.Titulo, Blocos = blocos });
        }

        if (outros.Count > 0)
            abas.Add(new AbaShpNot
            {
                Chave = "outros",
                Titulo = "Outros campos",
                Blocos =
                [
                    new NoShpNot { Titulo = "Sem correspondência na receção", Tabela = "GEODT01SPN", Campos = outros },
                ],
            });

        abas.Add(new AbaShpNot
        {
            Chave = "envio-estado",
            Titulo = "Estado do envio",
            Blocos = [new NoShpNot { Titulo = "Filas", Tabela = "GEODT01SPN", Campos = controlo }],
        });

        var flag = Texto(linha.GetValueOrDefault("FLAGENV"));

        return new ShpNotDetalhe
        {
            Resumo = new ShpNotResumo
            {
                Sentido = SentidoShpNot.Saida,
                Idt = idt,
                Id = mpsId ?? "",
                MpsId = mpsId,
                RespServ = Texto(linha.GetValueOrDefault("TIPOSHP")),
                FlagAs400 = flag,
                Estado = Estado(flag),
                Volumes = int.TryParse(Texto(linha.GetValueOrDefault("MPSCOUNTX")), out var v) ? v : null,
                Recebido = ShpNotRepository.DataPublica(linha.GetValueOrDefault("DATAHORA_INSERT")),
                ProcessadoAs400 = ShpNotRepository.DataPublica(linha.GetValueOrDefault("DATAHORAENV")),
                Erro = Estado(flag) == "erro" ? Texto(linha.GetValueOrDefault("RESPSERV")) : null,
            },
            Abas = abas,
        };
    }

    /// <summary>
    /// Os volumes de um envio. A ligação é <c>MPSMASTER = MPSIDX</c>, e a tabela guarda os
    /// volumes <b>além do primeiro</b> — o primeiro é o próprio envio, com o mesmo número.
    /// </summary>
    private async Task<NoShpNot> VolumesAsync(
        OracleConnection ligacao, string? mpsId, CancellationToken ct)
    {
        var linhas = new List<LinhaShpNot>();

        if (!string.IsNullOrWhiteSpace(mpsId))
        {
            await using var cmd = Comando(ligacao, $"""
                select * from {Volumes} where MPSMASTER = :mps
                 order by nvl2(PARCELRAN, 0, 1), lpad(trim(PARCELRAN), 12, '0')
                """);
            cmd.Parameters.Add("mps", OracleDbType.Varchar2, mpsId, ParameterDirection.Input);

            await using var leitor = await cmd.ExecuteReaderAsync(ct);
            while (await leitor.ReadAsync(ct))
            {
                var linha = Ler(leitor);
                linhas.Add(new LinhaShpNot
                {
                    Id = Texto(linha.GetValueOrDefault("IDT")) ?? Guid.NewGuid().ToString(),
                    Rotulo = Texto(linha.GetValueOrDefault("MPSID")),
                    Campos = [.. linha.Select(c => _catalogo.DescreverSaida("GEODT02SPN", c.Key, Texto(c.Value)))],
                });
            }
        }

        return new NoShpNot
        {
            Titulo = "Volumes",
            Tabela = "GEODT02SPN",
            Colecao = true,
            Linhas = linhas,
            Vazio = linhas.Count == 0,
        };
    }

    // ---------------------------------------------------------------- leitura crua

    private static Dictionary<string, object> Ler(OracleDataReader leitor)
    {
        var linha = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < leitor.FieldCount; i++)
            linha[leitor.GetName(i).ToUpperInvariant()] = leitor.GetValue(i);
        return linha;
    }

    /// <summary>
    /// As colunas destas tabelas são CHAR/VARCHAR2 de largura fixa vindos do AS400: sem cortar
    /// os brancos à direita, os valores chegam ao ecrã com uma cauda de espaços.
    /// </summary>
    private static string? Texto(object? valor) => valor switch
    {
        null or DBNull => null,
        DateTime d => d.ToString("yyyy-MM-dd HH:mm:ss"),
        _ => valor.ToString()?.TrimEnd() is { Length: > 0 } s ? s : null,
    };
}
