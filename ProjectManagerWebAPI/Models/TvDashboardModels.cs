namespace ProjectManagerWebAPI.Models;

/// <summary>
/// Resposta do mural. O envelope é fixo; o conteúdo vive em <see cref="Fontes"/>,
/// um bloco por origem de dados (ver ITvFonte). Assim acrescentar uma fonte não
/// obriga a mexer no contrato nem no ecrã.
/// </summary>
public class TvRespostaDto
{
    public DateTime GeradoEm { get; set; }
    public int RefreshSegundos { get; set; }

    /// <summary>Chave da fonte → o seu bloco de dados.</summary>
    public Dictionary<string, object> Fontes { get; set; } = [];

    /// <summary>Fontes que falharam neste ciclo, para o ecrã poder assinalá-lo.</summary>
    public List<string> FontesEmFalha { get; set; } = [];
}

/// <summary>Uma categoria e a sua contagem — serve barras, anéis e listas de contagem.</summary>
public class TvFatiaDto
{
    public string Rotulo { get; set; } = string.Empty;
    public int Total { get; set; }
}

// --- fonte: projetos ---

public class TvProjetosDto
{
    public int TotalProjetos { get; set; }
    public int ProjetosAtivos { get; set; }
    public int ProjetosConcluidos { get; set; }
    public int ProjetosAtrasados { get; set; }
    /// <summary>Percentagem de projetos ativos cuja data de fim ainda não passou.</summary>
    public int TaxaNoPrazo { get; set; }
    public int TarefasAbertas { get; set; }
    public int TarefasAtrasadas { get; set; }
    public int ConcluidosEsteMes { get; set; }

    public List<TvFatiaDto> PorEstado { get; set; } = [];
    public List<TvFatiaDto> PorSetor { get; set; } = [];
    public List<TvFatiaDto> CargaPorResponsavel { get; set; } = [];
    public List<TvProjetoLinhaDto> EmCurso { get; set; } = [];
    public List<TvTarefaLinhaDto> TarefasEmAtraso { get; set; } = [];
    public List<TvTarefaLinhaDto> TarefasProximas { get; set; } = [];
}

public class TvProjetoLinhaDto
{
    public string Nome { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public string Setor { get; set; } = string.Empty;
    public string Responsavel { get; set; } = string.Empty;
    public int Progresso { get; set; }
    public DateTime? Fim { get; set; }
    public int? DiasRestantes { get; set; }
    public bool Atrasado { get; set; }
}

public class TvTarefaLinhaDto
{
    public string Titulo { get; set; } = string.Empty;
    public string Projeto { get; set; } = string.Empty;
    public string Responsavel { get; set; } = string.Empty;
    public string Prioridade { get; set; } = string.Empty;
    public DateTime Prazo { get; set; }
    /// <summary>Negativo quando já passou o prazo.</summary>
    public int DiasParaPrazo { get; set; }
}

// --- fonte: seur ---

public class TvSeurDto
{
    public int GuiasHoje { get; set; }
    public int GuiasOntem { get; set; }
    /// <summary>FlagAtlas = Y.</summary>
    public int Enviadas { get; set; }
    /// <summary>FlagAtlas = E.</summary>
    public int ComErro { get; set; }
    /// <summary>FlagAtlas = N.</summary>
    public int PorEnviar { get; set; }
    public long Volumes { get; set; }
    public int TaxaEnvio { get; set; }
    public int VerifyPendente { get; set; }
    public int ErrosHoje { get; set; }

    /// <summary>Recolhas marcadas hoje em Portugal (PICKUPNUMBER a começar por X).</summary>
    public int RecolhasHoje { get; set; }

    public List<TvFatiaDto> GuiasPorHora { get; set; } = [];
    public List<TvFatiaDto> RecolhasPorHora { get; set; } = [];
    public List<TvFatiaDto> ErrosPorTipo { get; set; } = [];
    public List<TvFatiaDto> TopContas { get; set; } = [];
    public List<TvErroDto> UltimosErros { get; set; } = [];
}

public class TvErroDto
{
    public string Referencia { get; set; } = string.Empty;
    public string Titulo { get; set; } = string.Empty;
    public string? Detalhe { get; set; }
    public DateTime? Quando { get; set; }
}

// --- fonte: shpnot (GROUPSHPNOT.GEODT01SPN) ---

public class TvShpnotDto
{
    /// <summary>Hoje e ontem, por esta ordem.</summary>
    public List<TvShpnotDiaDto> Dias { get; set; } = [];

    /// <summary>Envios com sucesso hoje, pela hora a que saíram.</summary>
    public List<TvFatiaDto> EnviadosPorHora { get; set; } = [];
}

public class TvShpnotDiaDto
{
    public DateTime Dia { get; set; }
    /// <summary>"Hoje" ou "Ontem" — o mural mostra isto, não a data.</summary>
    public string Rotulo { get; set; } = string.Empty;

    public int Total { get; set; }

    // FLAGENV
    public int Sucesso { get; set; }
    public int Erro { get; set; }
    public int OutrosEstados { get; set; }

    // DATAHORAENV vs DATAHORA_INSERT
    public int EnviadosNoProprioDia { get; set; }
    public int EnviadosNoutroDia { get; set; }
    public int SemDataEnvio { get; set; }

    // SPTDATTIMX (YYYYMMDDHH24MISS)
    public int SptDentroDoDia { get; set; }
    public int SptForaDoDia { get; set; }
    /// <summary>Valores que não são uma data no formato esperado.</summary>
    public int SptFormatoInvalido { get; set; }

    // MPSIDX
    /// <summary>Linhas a mais por MPSIDX repetido (total − distintos).</summary>
    public int Duplicados { get; set; }
    /// <summary>Quantos MPSIDX aparecem mais do que uma vez.</summary>
    public int MpsidxRepetidos { get; set; }
    /// <summary>Total já sem duplicados — a base do nacional/internacional.</summary>
    public int Unicos { get; set; }

    public int Nacional { get; set; }
    public int Internacional { get; set; }
}

// --- fonte: tteventos (DPDIT.GEODT01TT) ---

public class TvTteventosDto
{
    /// <summary>Hoje e ontem, por esta ordem.</summary>
    public List<TvTteventosDiaDto> Dias { get; set; } = [];

    /// <summary>Envios de hoje, hora a hora, pela hora em que foram enviados.</summary>
    public List<TvTteventosHoraDto> PorHora { get; set; } = [];

    /// <summary>Estado da fila por enviar, medido em toda a tabela.</summary>
    public TvTteventosFilaDto Fila { get; set; } = new();
}

/// <summary>
/// A fila de eventos por enviar. Conta-se sobre a tabela inteira, e não sobre a
/// janela de dois dias: um evento encravado há uma semana é exatamente o que
/// interessa ver, e seria o primeiro a escapar a uma janela curta.
/// </summary>
public class TvTteventosFilaDto
{
    public int Pendentes { get; set; }
    /// <summary>Quando o mais antigo por enviar chegou à base de dados.</summary>
    public DateTime? MaisAntigoChegouEm { get; set; }
    /// <summary>Minutos entre a chegada do mais antigo e agora.</summary>
    public int AtrasoMinutos { get; set; }

    /// <summary>Tipo de evento (SCANCODEX) com mais eventos parados na fila.</summary>
    public string EventoComMais { get; set; } = "—";
    public int EventoComMaisTotal { get; set; }

    /// <summary>A fila repartida por tipo de evento, do maior para o menor.</summary>
    public List<TvFatiaDto> PorEvento { get; set; } = [];
}

public class TvTteventosDiaDto
{
    public DateTime Dia { get; set; }
    public string Rotulo { get; set; } = string.Empty;

    public int Total { get; set; }
    /// <summary>FLAGENV = Y.</summary>
    public int Sucesso { get; set; }
    /// <summary>FLAGENV = E.</summary>
    public int Erro { get; set; }
    /// <summary>FLAGENV = N — ainda na fila, sem carimbo de envio.</summary>
    public int PorEnviar { get; set; }
    public int OutrosEstados { get; set; }

    /// <summary>Percentagem de sucesso sobre o que já foi tentado (Y + E).</summary>
    public int TaxaSucesso { get; set; }
}

public class TvTteventosHoraDto
{
    /// <summary>"00h" … "23h".</summary>
    public string Rotulo { get; set; } = string.Empty;
    public int Enviados { get; set; }
    public int Sucesso { get; set; }
    public int Erro { get; set; }
}

// --- fonte: tracing (CHRONO_WEB.CW_PT_AS400_NEW_PORTAL) ---

public class TvTracingDto
{
    /// <summary>Hoje e ontem, por esta ordem.</summary>
    public List<TvTracingDiaDto> Dias { get; set; } = [];
}

public class TvTracingDiaDto
{
    /// <summary>Dia em formato YYYYMMDD, como está gravado em HHPDATINC.</summary>
    public int Dia { get; set; }
    public string Rotulo { get; set; } = string.Empty;

    public int Total { get; set; }

    // FLAGDPDGO: 'Y' já foi ao DPD Go, nulo ainda não.
    public int DpdGoEnviados { get; set; }
    public int DpdGoPendentes { get; set; }
    public int DpdGoTaxa { get; set; }

    // HHPCONFIRM: 'Y' enviado ao Portal, 'N' pendente, 'E' erro.
    public int PortalEnviados { get; set; }
    public int PortalPendentes { get; set; }
    public int PortalErro { get; set; }
    public int PortalTaxa { get; set; }
}

// --- fonte: as400 (OPERACOES.HHP000 no AS400 vs CW_PT_AS400_NEW_PORTAL no Oracle) ---

public class TvAs400Dto
{
    /// <summary>Hoje primeiro, depois os três dias anteriores.</summary>
    public List<TvAs400DiaDto> Dias { get; set; } = [];

    /// <summary>Falso quando o AS400 não respondeu — o mural mostra-o em vez de fingir zeros.</summary>
    public bool As400Disponivel { get; set; } = true;
}

public class TvAs400DiaDto
{
    /// <summary>Dia em YYYYMMDD, como está gravado em HHPDAT dos dois lados.</summary>
    public int Dia { get; set; }
    public string Rotulo { get; set; } = string.Empty;

    /// <summary>Registos no AS400, já sem HHPROWID repetidos.</summary>
    public int As400 { get; set; }
    /// <summary>Linhas a mais no AS400 por HHPROWID repetido.</summary>
    public int As400Duplicados { get; set; }

    /// <summary>Registos no Oracle, já sem HHPROWID repetidos.</summary>
    public int Oracle { get; set; }
    public int OracleDuplicados { get; set; }

    /// <summary>AS400 − Oracle. Positivo = o Oracle está atrás.</summary>
    public int EmFalta { get; set; }

    /// <summary>Percentagem do AS400 que já chegou ao Oracle.</summary>
    public int Cobertura { get; set; }
}
