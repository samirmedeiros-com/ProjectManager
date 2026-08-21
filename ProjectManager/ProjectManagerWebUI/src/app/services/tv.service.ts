import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface TvFatia {
  rotulo: string;
  total: number;
  /** Pinta a barra como problema — usado para marcar a hora em que houve erros. */
  alerta?: boolean;
}

// --- fonte: projetos ---

export interface TvProjetoLinha {
  nome: string;
  estado: string;
  setor: string;
  responsavel: string;
  progresso: number;
  fim: string | null;
  diasRestantes: number | null;
  atrasado: boolean;
}

export interface TvTarefaLinha {
  titulo: string;
  projeto: string;
  responsavel: string;
  prioridade: string;
  prazo: string;
  diasParaPrazo: number;
}

export interface TvProjetos {
  totalProjetos: number;
  projetosAtivos: number;
  projetosConcluidos: number;
  projetosAtrasados: number;
  taxaNoPrazo: number;
  tarefasAbertas: number;
  tarefasAtrasadas: number;
  concluidosEsteMes: number;
  porEstado: TvFatia[];
  porSetor: TvFatia[];
  cargaPorResponsavel: TvFatia[];
  emCurso: TvProjetoLinha[];
  tarefasEmAtraso: TvTarefaLinha[];
  tarefasProximas: TvTarefaLinha[];
}

// --- fonte: seur ---

export interface TvErro {
  referencia: string;
  titulo: string;
  detalhe: string | null;
  quando: string | null;
}

export interface TvSeur {
  guiasHoje: number;
  guiasOntem: number;
  enviadas: number;
  comErro: number;
  porEnviar: number;
  volumes: number;
  taxaEnvio: number;
  verifyPendente: number;
  errosHoje: number;
  /** Recolhas marcadas hoje em Portugal (PICKUPNUMBER a começar por X). */
  recolhasHoje: number;
  recolhasPorHora: TvFatia[];
  guiasPorHora: TvFatia[];
  errosPorTipo: TvFatia[];
  topContas: TvFatia[];
  ultimosErros: TvErro[];
}

// --- fonte: shpnot (GROUPSHPNOT.GEODT01SPN) ---

export interface TvShpnotDia {
  dia: string;
  /** "Hoje" ou "Ontem". */
  rotulo: string;
  total: number;

  // FLAGENV
  sucesso: number;
  erro: number;
  outrosEstados: number;

  // DATAHORAENV vs DATAHORA_INSERT
  enviadosNoProprioDia: number;
  enviadosNoutroDia: number;
  semDataEnvio: number;

  // SPTDATTIMX
  sptDentroDoDia: number;
  sptForaDoDia: number;
  sptFormatoInvalido: number;

  // MPSIDX
  duplicados: number;
  mpsidxRepetidos: number;
  unicos: number;

  nacional: number;
  internacional: number;
}

export interface TvShpnot {
  dias: TvShpnotDia[];
}

// --- fonte: tteventos (DPDIT.GEODT01TT) ---

export interface TvTteventosDia {
  dia: string;
  rotulo: string;
  total: number;
  /** FLAGENV = Y. */
  sucesso: number;
  /** FLAGENV = E. */
  erro: number;
  /** FLAGENV = N — ainda na fila. */
  porEnviar: number;
  outrosEstados: number;
  /** Sucesso sobre o que já foi tentado (Y + E). */
  taxaSucesso: number;
}

export interface TvTteventosHora {
  rotulo: string;
  enviados: number;
  sucesso: number;
  erro: number;
}

/** Estado da fila por enviar, medido em toda a tabela e não só na janela do dia. */
export interface TvTteventosFila {
  pendentes: number;
  /** Quando o mais antigo por enviar chegou à base de dados. */
  maisAntigoChegouEm: string | null;
  atrasoMinutos: number;
  /** Tipo de evento (SCANCODEX) com mais eventos parados na fila. */
  eventoComMais: string;
  eventoComMaisTotal: number;
  /** A fila repartida por tipo de evento, do maior para o menor (top 5). */
  porEvento: TvFatia[];
}

export interface TvTteventos {
  dias: TvTteventosDia[];
  porHora: TvTteventosHora[];
  fila: TvTteventosFila;
}

// --- fonte: tracing (CHRONO_WEB.CW_PT_AS400_NEW_PORTAL) ---

export interface TvTracingDia {
  /** YYYYMMDD, como está gravado em HHPDATINC. */
  dia: number;
  rotulo: string;
  total: number;

  /** FLAGDPDGO: 'Y' já foi ao DPD Go, nulo ainda não. */
  dpdGoEnviados: number;
  dpdGoPendentes: number;
  dpdGoTaxa: number;

  /** HHPCONFIRM: 'Y' enviado ao Portal, 'N' pendente, 'E' erro. */
  portalEnviados: number;
  portalPendentes: number;
  portalErro: number;
  portalTaxa: number;
}

export interface TvTracing {
  dias: TvTracingDia[];
}

// --- fonte: as400 (OPERACOES.HHP000 vs CW_PT_AS400_NEW_PORTAL) ---

export interface TvAs400Dia {
  /** YYYYMMDD, o HHPDAT que ambos os lados têm. */
  dia: number;
  rotulo: string;
  /** Registos no AS400, já sem HHPROWID repetidos. */
  as400: number;
  as400Duplicados: number;
  /** Registos no Oracle, já sem HHPROWID repetidos. */
  oracle: number;
  oracleDuplicados: number;
  /** AS400 − Oracle. Positivo = o Oracle está atrás. */
  emFalta: number;
  cobertura: number;
}

export interface TvAs400 {
  dias: TvAs400Dia[];
  /** Falso quando o AS400 não respondeu — os números do lado dele não valem. */
  as400Disponivel: boolean;
}

// --- envelope ---

export interface TvFontes {
  projetos?: TvProjetos;
  seur?: TvSeur;
  shpnot?: TvShpnot;
  tteventos?: TvTteventos;
  tracing?: TvTracing;
  as400?: TvAs400;
  /** Fontes acrescentadas mais tarde chegam aqui sem alterar este ficheiro. */
  [chave: string]: unknown;
}

export interface TvResposta {
  geradoEm: string;
  refreshSegundos: number;
  fontes: TvFontes;
  /** Fontes que falharam neste ciclo; os seus cards ficam de fora. */
  fontesEmFalha: string[];
}

@Injectable({ providedIn: 'root' })
export class TvService {
  constructor(private http: HttpClient) {}

  /**
   * O mural não tem sessão: a chave viaja no parâmetro `k`, tal como no URL que
   * a TV tem aberto. O auth.interceptor ignora `/api/tv/` de propósito.
   */
  obterDashboard(chave: string): Observable<TvResposta> {
    const params = new HttpParams().set('k', chave);
    return this.http.get<TvResposta>(`${environment.apiUrl}/api/tv/dashboard`, { params });
  }
}
