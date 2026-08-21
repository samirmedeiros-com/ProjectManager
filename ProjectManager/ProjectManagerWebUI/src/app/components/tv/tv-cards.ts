import { TvErro, TvFatia, TvProjetoLinha, TvResposta, TvSeur, TvShpnot, TvShpnotDia, TvTarefaLinha, TvTracing, TvTracingDia, TvTteventos, TvAs400, TvAs400Dia } from '../../services/tv.service';

/**
 * ---------------------------------------------------------------------------
 * DEFINIÇÃO DOS CARDS DO MURAL
 * ---------------------------------------------------------------------------
 * Este ficheiro é o único que precisas de tocar para mudar o que aparece na TV.
 *
 * O mural é um só ecrã, dividido em **secções** (ver SECCOES) — uma faixa por
 * assunto, com o seu cabeçalho. Assim vê-se de longe onde acaba um assunto e
 * começa o outro, sem ter de ler os títulos dos cards.
 *
 * Dentro de cada secção há uma grelha de **12 colunas**. As colunas de cada fila
 * têm de somar 12, e as linhas dos cards da secção têm de somar o `linhas` que a
 * secção declara — acima disso o conteúdo é cortado sem aviso. Os cards são
 * desenhados pela ordem do array, dentro da sua secção.
 *
 * Tipos disponíveis:
 *   'kpi'       → um número grande com legenda          (dados: TvKpi)
 *   'kpis'      → dois ou três números no mesmo card    (dados: TvKpiNomeado[])
 *   'barras'    → barras horizontais, para categorias   (dados: TvFatia[])
 *   'colunas'   → barras verticais, para séries no tempo(dados: TvFatia[])
 *   'projetos'  → tabela de projetos com progresso      (dados: TvProjetoLinha[])
 *   'tarefas'   → tabela de tarefas com prazo           (dados: TvTarefaLinha[])
 *   'erros'     → tabela de erros com referência e hora (dados: TvErro[])
 *   'comparativo' → blocos de métricas por período         (dados: TvComparativo)
 *
 * `fonte` diz de que bloco de dados o card depende ('projetos', 'seur', …).
 * Se essa fonte falhar num ciclo, só os cards dela é que saem do ecrã — os
 * outros continuam. `dados` recebe a resposta e devolve o que o card mostra.
 *
 * Se precisares de um número que a API ainda não calcula, acrescenta-o à fonte
 * respetiva em `Services/Tv/` no backend e ao DTO correspondente.
 */

export type TvTipoCard = 'kpi' | 'kpis' | 'barras' | 'colunas' | 'projetos' | 'tarefas' | 'erros' | 'comparativo';

/** Cor de destaque do card. */
export type TvTom = 'neutro' | 'bom' | 'aviso' | 'mau';

export interface TvKpi {
  valor: number | string;
  /** Texto pequeno por baixo do número (ex.: "de 24 no total"). */
  detalhe?: string;
  sufixo?: string;
  tom?: TvTom;
}

/** Uma linha de um card comparativo: o mesmo indicador em cada coluna. */
export interface TvLinhaComparativa {
  rotulo: string;
  /** Um valor por coluna, na ordem de `colunasComparativo`. */
  valores: number[];
  tom?: TvTom;
}

/** Um conjunto de métricas relacionadas, desenhado como um bloco próprio. */
export interface TvGrupoComparativo {
  titulo: string;
  linhas: TvLinhaComparativa[];
}

export interface TvComparativo {
  /** Cabeçalhos das colunas de valores (ex.: ['Hoje', 'Ontem']). */
  colunas: string[];
  /** Os grupos ficam lado a lado — numa TV lê-se melhor em largura do que em
      altura, e evita a lista vertical de quinze linhas que não cabia no ecrã. */
  grupos: TvGrupoComparativo[];
}

/** Um valor dentro de um card 'kpis' — precisa de rótulo para se distinguir. */
export interface TvKpiNomeado extends TvKpi {
  rotulo: string;
}

export type TvDadosCard = TvKpiNomeado[] | TvKpi | TvFatia[] | TvProjetoLinha[] | TvTarefaLinha[] | TvErro[] | TvComparativo;

/**
 * As faixas do mural, pela ordem em que aparecem. `linhas` é o orçamento vertical
 * da secção e também o seu peso: uma secção de 6 linhas fica com o dobro da altura
 * de uma de 3.
 */
export const SECCOES: TvSeccao[] = [
  { id: 'seur', titulo: 'Operação SEUR', linhas: 5 },
  { id: 'tt', titulo: 'TTEventos', linhas: 4 },
  { id: 'shpnot', titulo: 'Envios SHPNOT', linhas: 5 },
  { id: 'tracing', titulo: 'Tracing Público', linhas: 6 }
];

export interface TvSeccao {
  id: string;
  titulo: string;
  /** Orçamento de linhas da grelha desta secção, e o seu peso na altura do ecrã. */
  linhas: number;
}

export interface TvCard {
  /** Identificador estável — usado como chave de render. */
  id: string;
  /** Faixa do mural onde o card entra (ver SECCOES). */
  seccao: string;
  titulo: string;
  tipo: TvTipoCard;
  /** Bloco de dados de que este card depende. */
  fonte: 'projetos' | 'seur' | 'shpnot' | 'tteventos' | 'tracing' | 'as400';
  /** Largura em colunas de uma grelha de 12. */
  colunas: number;
  /** Altura em linhas da grelha. */
  linhas: number;
  dados: (d: TvResposta) => TvDadosCard;
  /** Texto mostrado quando o card não tem nada a listar. */
  vazio?: string;
}

/** Atalhos para não repetir o caminho até cada bloco. */
const seur = (d: TvResposta) => d.fontes.seur as TvSeur;
const shpnot = (d: TvResposta) => d.fontes.shpnot as TvShpnot;
const tt = (d: TvResposta) => d.fontes.tteventos as TvTteventos;
const tracing = (d: TvResposta) => d.fontes.tracing as TvTracing;
const as400 = (d: TvResposta) => d.fontes.as400 as TvAs400;

/** Hora do dia a partir de um instante ISO, para os detalhes dos cards. */
const horaDe = (iso: string): string =>
  new Date(iso).toLocaleTimeString('pt-PT', { hour: '2-digit', minute: '2-digit' });

/** O backend devolve hoje primeiro e ontem a seguir. */
const hoje = (d: TvResposta): TvShpnotDia | undefined => shpnot(d).dias[0];
const ontem = (d: TvResposta): TvShpnotDia | undefined => shpnot(d).dias[1];

export const CARDS: TvCard[] = [

  // ========== Secção SEUR (5 linhas) ==========
  // Duas filas, cada uma a somar 12 colunas:
  //   guias    (3 linhas)  2 + 2 + 2 + 6 = 12  → indicadores estreitos e o ritmo
  //   recolhas (2 linhas)  3 + 9         = 12  → o total do dia e o ritmo ao lado
  //
  // A segunda fila é de 2 linhas e não de 3 de propósito: o mural é um ecrã fixo,
  // e cada linha que esta secção ganha sai das outras. Com 3 os comparativos do
  // SHPNOT e do Tracing perdiam a última linha da tabela — verificado no ecrã.

  {
    id: 'seur-guias',
    seccao: 'seur',
    titulo: 'Guias hoje',
    tipo: 'kpi',
    fonte: 'seur',
    colunas: 2,
    linhas: 3,
    dados: (d): TvKpi => ({
      valor: seur(d).guiasHoje,
      detalhe: `ontem ${seur(d).guiasOntem}`
    })
  },
  {
    id: 'seur-taxa',
    seccao: 'seur',
    titulo: 'Enviadas ao Atlas',
    tipo: 'kpi',
    fonte: 'seur',
    colunas: 2,
    linhas: 3,
    dados: (d): TvKpi => ({
      valor: seur(d).taxaEnvio,
      sufixo: '%',
      detalhe: `${seur(d).enviadas} enviadas`,
      tom: seur(d).taxaEnvio >= 98 ? 'bom' : seur(d).taxaEnvio >= 90 ? 'aviso' : 'mau'
    })
  },
  {
    id: 'seur-erro',
    seccao: 'seur',
    titulo: 'Guias com erro',
    tipo: 'kpi',
    fonte: 'seur',
    colunas: 2,
    linhas: 3,
    dados: (d): TvKpi => ({
      valor: seur(d).comErro,
      detalhe: `${seur(d).porEnviar} por enviar`,
      tom: seur(d).comErro > 0 ? 'mau' : 'bom'
    })
  },
  {
    id: 'seur-ritmo',
    seccao: 'seur',
    titulo: 'Guias por hora',
    tipo: 'colunas',
    fonte: 'seur',
    colunas: 6,
    linhas: 3,
    dados: (d) => seur(d).guiasPorHora,
    vazio: 'Sem guias hoje.'
  },
  {
    id: 'seur-recolhas',
    seccao: 'seur',
    titulo: 'Recolhas marcadas pela SEUR em Portugal',
    tipo: 'kpi',
    fonte: 'seur',
    colunas: 3,
    linhas: 2,
    // Recolhas marcadas em Portugal, contadas pela hora em que foram marcadas.
    dados: (d): TvKpi => {
      const s = seur(d);
      const horas = s.recolhasPorHora ?? [];
      const ultima = horas[horas.length - 1];

      return {
        valor: s.recolhasHoje ?? 0,
        detalhe: ultima ? `${ultima.total} em ${ultima.rotulo}` : 'sem marcações hoje'
      };
    }
  },
  {
    id: 'seur-recolhas-ritmo',
    seccao: 'seur',
    titulo: 'Recolhas por hora',
    tipo: 'colunas',
    fonte: 'seur',
    colunas: 9,
    linhas: 2,
    dados: (d) => seur(d).recolhasPorHora ?? [],
    vazio: 'Sem recolhas marcadas hoje.'
  },

  // ========== Secção TTEventos (4 linhas) ==========
  // Mesma forma da SEUR: indicadores estreitos e o ritmo do dia ao lado.
  // 2 + 2 + 2 + 6 = 12 colunas.

  {
    id: 'tt-eventos',
    seccao: 'tt',
    titulo: 'Eventos hoje',
    tipo: 'kpi',
    fonte: 'tteventos',
    colunas: 2,
    linhas: 4,
    dados: (d): TvKpi => {
      const dias = tt(d).dias;
      return {
        valor: dias[0]?.total ?? 0,
        detalhe: `ontem ${dias[1]?.total ?? 0}`
      };
    }
  },
  {
    id: 'tt-envios',
    seccao: 'tt',
    titulo: 'Envios',
    tipo: 'kpis',
    fonte: 'tteventos',
    colunas: 2,
    linhas: 4,
    dados: (d): TvKpiNomeado[] => {
      const h = tt(d).dias[0];
      return [
        {
          rotulo: 'Com sucesso',
          valor: h?.sucesso ?? 0,
          detalhe: `${h?.taxaSucesso ?? 100}% do tentado`,
          tom: 'bom'
        },
        {
          rotulo: 'Com erro',
          valor: h?.erro ?? 0,
          tom: (h?.erro ?? 0) > 0 ? 'mau' : 'bom'
        }
      ];
    }
  },
  {
    id: 'tt-por-enviar',
    seccao: 'tt',
    titulo: 'Fila por enviar',
    tipo: 'kpis',
    fonte: 'tteventos',
    colunas: 3,
    linhas: 4,
    dados: (d): TvKpiNomeado[] => {
      const f = tt(d).fila;

      return [
        {
          rotulo: 'Por enviar',
          valor: f.pendentes,
          tom: f.pendentes > 0 ? 'aviso' : 'bom'
        },
        {
          rotulo: 'Atraso',
          valor: f.atrasoMinutos,
          sufixo: ' min',
          detalhe: f.maisAntigoChegouEm ? `desde ${horaDe(f.maisAntigoChegouEm)}` : 'vazia',
          // Trinta minutos é o limiar a partir do qual a fila deixa de parecer
          // um lote normal e começa a parecer um bloqueio.
          tom: f.atrasoMinutos >= 60 ? 'mau' : f.atrasoMinutos >= 30 ? 'aviso' : 'bom'
        },
        {
          rotulo: 'Evento com mais',
          valor: f.eventoComMaisTotal,
          detalhe: f.eventoComMais,
          tom: 'neutro'
        }
      ];
    }
  },
  {
    id: 'tt-ritmo',
    seccao: 'tt',
    titulo: 'Enviados por hora',
    tipo: 'colunas',
    fonte: 'tteventos',
    colunas: 5,
    linhas: 4,
    // A barra fica marcada nas horas em que houve erro — assim vê-se de relance
    // *quando* correu mal, não só que correu mal.
    dados: (d): TvFatia[] =>
      tt(d).porHora.map((h) => ({ rotulo: h.rotulo, total: h.enviados, alerta: h.erro > 0 })),
    vazio: 'Nada enviado hoje.'
  },

  // ========== Secção SHPNOT (5 linhas) ==========
  // Fila 1 (2 linhas): total do dia e repartição por destino
  // Fila 2 (4 linhas): o comparativo hoje/ontem, à largura toda

  {
    id: 'shpnot-total',
    seccao: 'shpnot',
    titulo: 'Envios hoje',
    tipo: 'kpi',
    fonte: 'shpnot',
    colunas: 4,
    linhas: 2,
    dados: (d): TvKpi => ({
      valor: hoje(d)?.total ?? 0,
      detalhe: `ontem ${ontem(d)?.total ?? 0}`
    })
  },
  {
    id: 'shpnot-duplicados',
    seccao: 'shpnot',
    titulo: 'Duplicados MPSIDX',
    tipo: 'kpi',
    fonte: 'shpnot',
    colunas: 4,
    linhas: 2,
    dados: (d): TvKpi => {
      const h = hoje(d);
      return {
        valor: h?.duplicados ?? 0,
        detalhe: `${h?.unicos ?? 0} envios únicos`,
        tom: (h?.duplicados ?? 0) > 0 ? 'aviso' : 'bom'
      };
    }
  },
  {
    id: 'shpnot-destino',
    seccao: 'shpnot',
    titulo: 'Destino (hoje)',
    tipo: 'barras',
    fonte: 'shpnot',
    colunas: 4,
    linhas: 2,
    dados: (d): TvFatia[] => {
      const h = hoje(d);
      if (!h) return [];
      return [
        { rotulo: 'Nacional', total: h.nacional },
        { rotulo: 'Internacional', total: h.internacional }
      ];
    },
    vazio: 'Sem envios hoje.'
  },
  {
    id: 'shpnot-comparativo',
    seccao: 'shpnot',
    titulo: 'Hoje e ontem',
    tipo: 'comparativo',
    fonte: 'shpnot',
    colunas: 12,
    linhas: 3,
    dados: (d): TvComparativo => {
      const dias: TvShpnotDia[] = shpnot(d).dias;
      const v = (f: (x: TvShpnotDia) => number) => dias.map(f);

      return {
        colunas: dias.map((x) => x.rotulo),
        grupos: [
          {
            titulo: 'Estado',
            linhas: [
              { rotulo: 'Sucesso (Y)', valores: v((x) => x.sucesso), tom: 'bom' },
              { rotulo: 'Erro (N)', valores: v((x) => x.erro), tom: 'mau' },
              { rotulo: 'Total', valores: v((x) => x.total) }
            ]
          },
          {
            titulo: 'Envio',
            linhas: [
              { rotulo: 'No próprio dia', valores: v((x) => x.enviadosNoProprioDia) },
              { rotulo: 'Noutro dia', valores: v((x) => x.enviadosNoutroDia), tom: 'aviso' },
              { rotulo: 'Sem data', valores: v((x) => x.semDataEnvio), tom: 'aviso' }
            ]
          },
          {
            titulo: 'Scan',
            linhas: [
              { rotulo: 'Dentro do dia', valores: v((x) => x.sptDentroDoDia) },
              { rotulo: 'Fora do dia', valores: v((x) => x.sptForaDoDia), tom: 'aviso' },
              { rotulo: 'Formato inválido', valores: v((x) => x.sptFormatoInvalido), tom: 'mau' }
            ]
          },
          {
            titulo: 'Destino',
            linhas: [
              { rotulo: 'Nacional', valores: v((x) => x.nacional) },
              { rotulo: 'Internacional', valores: v((x) => x.internacional) },
              { rotulo: 'Duplicados MPSIDX', valores: v((x) => x.duplicados), tom: 'aviso' }
            ]
          }
        ]
      };
    },
    vazio: 'Sem envios na janela.'
  },

  // ========== Secção Tracing Público (4 linhas) ==========
  // Uma fila: o estado de cada canal e a comparação com ontem.
  // 3 + 3 + 6 = 12 colunas.

  {
    id: 'tracing-dpdgo',
    seccao: 'tracing',
    titulo: 'DPD Go',
    tipo: 'kpis',
    fonte: 'tracing',
    colunas: 3,
    linhas: 3,
    dados: (d): TvKpiNomeado[] => {
      const h = tracing(d).dias[0];
      return [
        { rotulo: 'Enviados', valor: h?.dpdGoEnviados ?? 0, detalhe: `${h?.dpdGoTaxa ?? 100}% do dia`, tom: 'bom' },
        {
          rotulo: 'Pendentes',
          valor: h?.dpdGoPendentes ?? 0,
          tom: (h?.dpdGoPendentes ?? 0) > 0 ? 'aviso' : 'bom'
        }
      ];
    }
  },
  {
    id: 'tracing-portal',
    seccao: 'tracing',
    titulo: 'Portal',
    tipo: 'kpis',
    fonte: 'tracing',
    colunas: 3,
    linhas: 3,
    dados: (d): TvKpiNomeado[] => {
      const h = tracing(d).dias[0];
      return [
        { rotulo: 'Enviados', valor: h?.portalEnviados ?? 0, detalhe: `${h?.portalTaxa ?? 100}% do dia`, tom: 'bom' },
        {
          rotulo: 'Pendentes',
          valor: h?.portalPendentes ?? 0,
          tom: (h?.portalPendentes ?? 0) > 0 ? 'aviso' : 'bom'
        },
        { rotulo: 'Com erro', valor: h?.portalErro ?? 0, tom: (h?.portalErro ?? 0) > 0 ? 'mau' : 'bom' }
      ];
    }
  },
  {
    id: 'tracing-comparativo',
    seccao: 'tracing',
    titulo: 'Hoje e ontem',
    tipo: 'comparativo',
    fonte: 'tracing',
    colunas: 6,
    linhas: 3,
    dados: (d): TvComparativo => {
      const dias: TvTracingDia[] = tracing(d).dias;
      const v = (f: (x: TvTracingDia) => number) => dias.map(f);

      return {
        colunas: dias.map((x) => x.rotulo),
        grupos: [
          {
            titulo: 'DPD Go',
            linhas: [
              { rotulo: 'Enviados', valores: v((x) => x.dpdGoEnviados), tom: 'bom' },
              { rotulo: 'Pendentes', valores: v((x) => x.dpdGoPendentes), tom: 'aviso' },
              { rotulo: 'Total', valores: v((x) => x.total) }
            ]
          },
          {
            titulo: 'Portal',
            linhas: [
              { rotulo: 'Enviados', valores: v((x) => x.portalEnviados), tom: 'bom' },
              { rotulo: 'Pendentes', valores: v((x) => x.portalPendentes), tom: 'aviso' },
              { rotulo: 'Com erro', valores: v((x) => x.portalErro), tom: 'mau' }
            ]
          }
        ]
      };
    },
    vazio: 'Sem registos na janela.'
  },

  // ----- AS400 vs Oracle, dentro do Tracing Público -----
  // O HHPDAT é o campo que os dois lados partilham. Os HHPROWID repetidos são
  // excluídos de ambos: o AS400 tem-nos, o Oracle não, e compará-los em bruto
  // mostraria uma diferença que não existe.

  {
    id: 'as400-hoje',
    seccao: 'tracing',
    titulo: 'AS400 vs Oracle (hoje)',
    tipo: 'kpis',
    fonte: 'as400',
    colunas: 3,
    linhas: 3,
    dados: (d): TvKpiNomeado[] => {
      const a = as400(d);
      const h = a.dias[0];

      if (!a.as400Disponivel) {
        return [{ rotulo: 'AS400', valor: '—', detalhe: 'sem resposta' }];
      }

      return [
        { rotulo: 'AS400', valor: h?.as400 ?? 0 },
        { rotulo: 'Oracle', valor: h?.oracle ?? 0 }
      ];
    }
  },
  {
    id: 'as400-falta',
    seccao: 'tracing',
    titulo: 'Por replicar (hoje)',
    tipo: 'kpi',
    fonte: 'as400',
    colunas: 3,
    linhas: 3,
    dados: (d): TvKpi => {
      const a = as400(d);
      const h = a.dias[0];

      if (!a.as400Disponivel) {
        return { valor: '—', detalhe: 'AS400 sem resposta', tom: 'aviso' };
      }

      return {
        valor: h?.emFalta ?? 0,
        detalhe: `${h?.cobertura ?? 100}% já no Oracle`,
        tom: (h?.emFalta ?? 0) === 0 ? 'bom' : (h?.cobertura ?? 100) >= 99 ? 'aviso' : 'mau'
      };
    }
  },
  {
    id: 'as400-dias',
    seccao: 'tracing',
    titulo: 'Por replicar (3 dias anteriores)',
    tipo: 'kpi',
    fonte: 'as400',
    colunas: 6,
    linhas: 3,
    // O dia de hoje fica de fora de propósito: ainda está a receber registos, e
    // a diferença medida a meio do dia não distingue um atraso de replicação de
    // uma replicação que simplesmente ainda não aconteceu. Nos dias já fechados
    // qualquer registo em falta é mesmo uma falha.
    //
    // A soma é do que falta em cada dia, não da diferença dos totais: um dia em
    // que o Oracle tenha mais registos não deve encobrir a falta noutro.
    dados: (d): TvKpi => {
      const a = as400(d);

      if (!a.as400Disponivel) {
        return { valor: '—', detalhe: 'AS400 sem resposta', tom: 'aviso' };
      }

      const fechados = a.dias.slice(1);
      if (fechados.length === 0) {
        return { valor: 0, detalhe: 'sem dias fechados', tom: 'neutro' };
      }

      const emFalta = fechados.reduce((soma, x) => soma + x.emFalta, 0);
      const noAs400 = fechados.reduce((soma, x) => soma + x.as400, 0);
      const cobertura = noAs400 === 0 ? 100 : Math.floor((100 * (noAs400 - emFalta)) / noAs400);
      // Pelas datas e não pelos rótulos: o de ontem é "Ontem" e daria um
      // intervalo com formatos misturados ("19/08 a Ontem").
      const diaMes = (yyyymmdd: number) =>
        `${String(yyyymmdd % 100).padStart(2, '0')}/${String(Math.floor(yyyymmdd / 100) % 100).padStart(2, '0')}`;
      const periodo = `${diaMes(fechados[fechados.length - 1].dia)} a ${diaMes(fechados[0].dia)}`;

      return {
        valor: emFalta,
        detalhe: `${periodo} · ${cobertura}% já no Oracle`,
        tom: emFalta === 0 ? 'bom' : cobertura >= 99 ? 'aviso' : 'mau'
      };
    }
  }
];
