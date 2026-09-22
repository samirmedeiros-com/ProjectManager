import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { SeurAuthService } from './seur-auth.service';

/**
 * Um campo de um SHPNOT. O valor é um; os nomes são três, porque o mesmo dado muda de nome
 * conforme quem fala dele: `json` é como veio do Geopost, `tabela`/`coluna` é onde ficou
 * guardado pelo WebApiShpNot, e `alias` é como sai na GROUPSHPNOT.VW_SHPNOT_AS400 a caminho
 * do AS400. É isso que o ecrã põe no hint de cada campo.
 */
export interface CampoShpNot {
  etiqueta: string;
  json: string | null;
  tabela: string;
  coluna: string;
  alias: string | null;
  valor: string | null;
  /** Coluna guardada como texto JSON (ex. sPartnerRefs): mostra-se como lista. */
  lista: boolean;
}

export interface LinhaShpNot {
  id: string;
  rotulo: string | null;
  campos: CampoShpNot[];
  filhos: NoShpNot[];
}

export interface NoShpNot {
  titulo: string;
  tabela: string;
  campos: CampoShpNot[];
  filhos: NoShpNot[];
  colecao: boolean;
  linhas: LinhaShpNot[];
  /** O SHPNOT não trouxe este bloco. Mostra-se à mesma, para se ver que existe e está vazio. */
  vazio: boolean;
}

export interface AbaShpNot {
  chave: string;
  titulo: string;
  blocos: NoShpNot[];
}

export type EstadoShpNot = 'enviado' | 'erro' | 'pendente';

export interface ShpNotResumo {
  idt: number;
  id: string;
  mpsId: string | null;
  remetente: string | null;
  destinatario: string | null;
  pais: string | null;
  volumes: number | null;
  respServ: string | null;
  /** A letra original da FLAGAS400 (Y/N/E) — fica à vista ao lado do estado. */
  flagAs400: string | null;
  estado: EstadoShpNot;
  recebido: string | null;
  processadoAs400: string | null;
}

export interface ShpNotDetalhe {
  resumo: ShpNotResumo;
  abas: AbaShpNot[];
}

/**
 * O estado da fila para o AS400 — e não um resumo do dia. Contar os envios de um dia obriga
 * a varrer a tabela toda (DATAINSERT não tem índice e entram ~80 mil por dia); estes números
 * saem por índice em dois segundos.
 */
export interface ShpNotEstatisticas {
  pendentes: number;
  erros: number;
  ultimoRecebido: string | null;
  ultimoIdt: number | null;
}

/** Uma fatia da listagem. Não há total: só se sabe se existe página seguinte. */
export interface FatiaShpNot {
  itens: ShpNotResumo[];
  paginaAtual: number;
  tamanho: number;
  haMais: boolean;
}

@Injectable({ providedIn: 'root' })
export class ShpNotService {
  // seurApiUrl e o /api completo, como nas contas e no trace push: o apiUrl sozinho não leva
  // o prefixo e os pedidos saíam para /shpnot, que não é rota nenhuma (404).
  private readonly api = `${environment.seurApiUrl}/api/shpnot`;

  constructor(private http: HttpClient, private auth: SeurAuthService) {}

  /**
   * O token vai à mão, como no resto da Gestão de Dados: o interceptor global usa a sessão do
   * Project Manager e estes ecrãs autenticam-se com as credenciais da Gestão SEUR.
   */
  private get cabecalhos(): HttpHeaders {
    return new HttpHeaders({ Authorization: `Bearer ${this.auth.getToken() ?? ''}` });
  }

  estatisticas(): Observable<ShpNotEstatisticas> {
    return this.http.get<ShpNotEstatisticas>(`${this.api}/estatisticas`, {
      headers: this.cabecalhos,
    });
  }

  procurar(filtro: {
    data?: string;
    mpsid?: string;
    volume?: string;
    estado?: string;
    respserv?: string;
    pagina: number;
    tamanho: number;
  }): Observable<FatiaShpNot> {
    let params = new HttpParams()
      .set('pagina', filtro.pagina)
      .set('tamanho', filtro.tamanho);

    if (filtro.data) params = params.set('data', filtro.data);
    if (filtro.mpsid?.trim()) params = params.set('mpsid', filtro.mpsid.trim());
    if (filtro.volume?.trim()) params = params.set('volume', filtro.volume.trim());
    if (filtro.estado) params = params.set('estado', filtro.estado);
    if (filtro.respserv?.trim()) params = params.set('respserv', filtro.respserv.trim());

    return this.http.get<FatiaShpNot>(this.api, { headers: this.cabecalhos, params });
  }

  obter(idt: number): Observable<ShpNotDetalhe> {
    return this.http.get<ShpNotDetalhe>(`${this.api}/${idt}`, { headers: this.cabecalhos });
  }
}
