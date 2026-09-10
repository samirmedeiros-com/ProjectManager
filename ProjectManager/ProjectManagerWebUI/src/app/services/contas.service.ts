import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { SeurAuthService } from './seur-auth.service';

export interface ContaResumo {
  idt: number | null;
  eeent: string | null;
  eenom: string | null;
  ftsit: string | null;
  flagportal: string | null;
  subContas: number;
}

export interface Conta {
  idt?: number | null;
  eeent: string | null;
  eenom: string | null;
  eenom2: string | null;
  eectb: string | null;
  eecae: string | null;
  ftsit: string | null;
  ftsgm: string | null;
  ftzon: string | null;
  ftven: string | null;
  flagportal?: string | null;
}

/**
 * Subconta tal como está na tabela: praticamente tudo é texto, mesmo o que parece número
 * ou sim/não, porque as colunas vêm do AS400 e é assim que o processo de envio as lê.
 */
export interface SubConta {
  idt?: number | null;
  ement: string | null;
  emend: string | null;
  emdsc: string | null;
  emmor: string | null;
  emloc: string | null;
  empos: string | null;
  empai: string | null;
  emenf: string | null;
  emtft: string | null;
  emexp: string | null;
  bicact: string | null;
  emtra: string | null;
  telefone: string | null;
  telemovel: string | null;
  email: string | null;
  serviceRc: string | null;
  serviceB2c: string | null;
  servFrio: string | null;
  bicsci: string | null;
  bicsnif: string | null;
  bicsccc: string | null;
  bicspraca: string | null;
  bicsccc2: string | null;
  bicsusr2: string | null;
  bicsserv: string | null;
  bicsprod: string | null;
  bicvcli: number | null;
  active: string | null;
  grassinada: string | null;
  predict: string | null;
  multiparcela: string | null;
  cod: string | null;
  servicoRcComCod: string | null;
  textoServico: string | null;
  templateEtiqueta: string | null;
  textEtiquetaAuxiliar: string | null;
  templateEtiquetaComCod: string | null;
  textEtiquetaComCod: string | null;
  reenAutoLoja: string | null;
  lojaAMostrar: string | null;
  pickupExpeditor: string | null;
  pickupDestinatario: string | null;
  moradaDestinatario: string | null;
  limitePeso: string | null;
  peso: string | null;
  inflightLojas: string | null;
  seInflightLojasLimVol: string | null;
  seInflightLojasLimP: string | null;
  infightData: string | null;
  inflightMorada: string | null;
  swap: string | null;
  tipoDestino: string | null;
  dimensoes: string | null;
  medidas: string | null;
  lockreppic: string | null;
  recprtdpdc: string | null;
  descServico: string | null;
  fresh: string | null;
  serviceFrio: string | null;
  flagportal?: string | null;
}

export interface Pagina<T> {
  itens: T[];
  total: number;
  paginaAtual: number;
  tamanho: number;
}

export interface EstatisticaTabela {
  transmitidas: number;
  aTransmitir: number;
  erros: number;
  outros: number;
  total: number;
}

export interface ContasEstatisticas {
  contas: EstatisticaTabela;
  subContas: EstatisticaTabela;
}

export interface PedidoEnvio {
  ambiente: 'PRD' | 'QUA';
  conta: string;
  subConta?: string | null;
}

export interface LinhaEnvio {
  tipo: string;
  identificacao: string;
  metodo: string;
  statusCode: number;
  sucesso: boolean;
  resposta: string | null;
}

export interface ResultadoEnvio {
  ambiente: string;
  sucesso: boolean;
  mensagem: string;
  linhas: LinhaEnvio[];
}

export interface EnvioLog {
  id: number;
  utilizador: string | null;
  ambiente: string;
  conta: string;
  subConta: string | null;
  sucesso: boolean;
  totalLinhas: number;
  falhas: number;
  mensagem: string | null;
  elapsedMs: number | null;
  criadoEm: string;
}

export interface EnvioLogLinha {
  tipo: string;
  identificacao: string;
  metodo: string;
  statusCode: number;
  sucesso: boolean;
  resposta: string | null;
  criadoEm: string;
}

@Injectable({ providedIn: 'root' })
export class ContasService {
  private apiUrl = `${environment.seurApiUrl}/api/contas`;

  constructor(private http: HttpClient, private seurAuth: SeurAuthService) {}

  /**
   * A Gestão de Dados usa as credenciais da Gestão SEUR: o Bearer é o `seur_token`, posto
   * aqui à mão como nos outros serviços do SEUR. O auth.interceptor do Project Manager
   * exclui `/api/contas/` de propósito — mandaria a credencial errada.
   */
  private h(): HttpHeaders {
    return new HttpHeaders({ Authorization: `Bearer ${this.seurAuth.getToken()}` });
  }

  acesso(): Observable<{ permitido: boolean; aplicacao: string }> {
    return this.http.get<{ permitido: boolean; aplicacao: string }>(`${this.apiUrl}/acesso`, { headers: this.h() });
  }

  estatisticas(): Observable<ContasEstatisticas> {
    return this.http.get<ContasEstatisticas>(`${this.apiUrl}/estatisticas`, { headers: this.h() });
  }

  listar(procura?: string, flagportal?: string, pagina = 1, tamanho = 50): Observable<Pagina<ContaResumo>> {
    let params = new HttpParams().set('pagina', String(pagina)).set('tamanho', String(tamanho));
    if (procura) params = params.set('procura', procura);
    if (flagportal) params = params.set('flagportal', flagportal);
    return this.http.get<Pagina<ContaResumo>>(this.apiUrl, { headers: this.h(), params });
  }

  obter(eeent: string): Observable<Conta> {
    return this.http.get<Conta>(`${this.apiUrl}/${encodeURIComponent(eeent)}`, { headers: this.h() });
  }

  gravar(conta: Conta): Observable<Conta> {
    return this.http.post<Conta>(this.apiUrl, conta, { headers: this.h() });
  }

  subContas(eeent: string): Observable<SubConta[]> {
    return this.http.get<SubConta[]>(`${this.apiUrl}/${encodeURIComponent(eeent)}/subcontas`, { headers: this.h() });
  }

  gravarSubConta(subConta: SubConta): Observable<SubConta> {
    return this.http.post<SubConta>(`${this.apiUrl}/subcontas`, subConta, { headers: this.h() });
  }

  enviar(pedido: PedidoEnvio): Observable<ResultadoEnvio> {
    return this.http.post<ResultadoEnvio>(`${this.apiUrl}/enviar`, pedido, { headers: this.h() });
  }

  logs(limite = 100, conta?: string): Observable<EnvioLog[]> {
    let params = new HttpParams().set('limite', String(limite));
    if (conta) params = params.set('conta', conta);
    return this.http.get<EnvioLog[]>(`${this.apiUrl}/logs`, { headers: this.h(), params });
  }

  logLinhas(logId: number): Observable<EnvioLogLinha[]> {
    return this.http.get<EnvioLogLinha[]>(`${this.apiUrl}/logs/${logId}/linhas`, { headers: this.h() });
  }
}
