import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { SeurAuthService } from './seur-auth.service';

export interface EstadoCluster {
  nome: string;
  versao: string;
  saude: string;
  totalIndices: number;
  totalDocumentos: number;
  tamanhoBytes: number;
}

export interface IndiceInfo {
  nome: string;
  saude: string;
  estado: string;
  documentos: number;
  tamanhoBytes: number;
}

export interface CampoInfo {
  nome: string;
  tipo: string;
  ordenavel: boolean;
  temporal: boolean;
}

export interface PedidoPesquisa {
  indice: string;
  consulta?: string;
  campoData?: string;
  de?: string;
  ate?: string;
  ordenarPor?: string;
  ordemDescendente: boolean;
  pagina: number;
  tamanho: number;
}

export interface DocumentoResultado {
  id: string;
  indice: string;
  score: number | null;
  campos: Record<string, unknown>;
}

export interface ResultadoPesquisa {
  total: number;
  totalExato: boolean;
  duracaoMs: number;
  colunas: string[];
  documentos: DocumentoResultado[];
}

@Injectable({ providedIn: 'root' })
export class OpenSearchService {
  private apiUrl = `${environment.seurApiUrl}/api/opensearch`;

  constructor(private http: HttpClient, private seurAuth: SeurAuthService) {}

  /**
   * Este portal usa as credenciais da Gestão SEUR: o Bearer é o `seur_token`, posto aqui
   * à mão como nos outros serviços do SEUR. O auth.interceptor do Project Manager exclui
   * `/api/opensearch/` de propósito — mandaria a credencial errada.
   */
  private h(): HttpHeaders {
    return new HttpHeaders({ Authorization: `Bearer ${this.seurAuth.getToken()}` });
  }

  /** Valida a sessão SEUR contra o servidor antes de abrir o ecrã. */
  acesso(): Observable<{ permitido: boolean; aplicacao: string }> {
    return this.http.get<{ permitido: boolean; aplicacao: string }>(`${this.apiUrl}/acesso`, { headers: this.h() });
  }

  estado(): Observable<EstadoCluster> {
    return this.http.get<EstadoCluster>(`${this.apiUrl}/estado`, { headers: this.h() });
  }

  indices(): Observable<IndiceInfo[]> {
    return this.http.get<IndiceInfo[]>(`${this.apiUrl}/indices`, { headers: this.h() });
  }

  campos(indice: string): Observable<CampoInfo[]> {
    return this.http.get<CampoInfo[]>(`${this.apiUrl}/indices/${encodeURIComponent(indice)}/campos`, { headers: this.h() });
  }

  pesquisar(pedido: PedidoPesquisa): Observable<ResultadoPesquisa> {
    return this.http.post<ResultadoPesquisa>(`${this.apiUrl}/pesquisa`, pedido, { headers: this.h() });
  }
}
