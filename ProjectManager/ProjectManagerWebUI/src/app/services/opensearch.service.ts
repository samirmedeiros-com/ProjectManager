import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

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
  private apiUrl = `${environment.apiUrl}/api/opensearch`;

  constructor(private http: HttpClient) {}

  /** Confirma que o utilizador pertence ao setor IT. O setor não vem no token. */
  acesso(): Observable<{ permitido: boolean; setor: string }> {
    return this.http.get<{ permitido: boolean; setor: string }>(`${this.apiUrl}/acesso`);
  }

  estado(): Observable<EstadoCluster> {
    return this.http.get<EstadoCluster>(`${this.apiUrl}/estado`);
  }

  indices(): Observable<IndiceInfo[]> {
    return this.http.get<IndiceInfo[]>(`${this.apiUrl}/indices`);
  }

  campos(indice: string): Observable<CampoInfo[]> {
    return this.http.get<CampoInfo[]>(`${this.apiUrl}/indices/${encodeURIComponent(indice)}/campos`);
  }

  pesquisar(pedido: PedidoPesquisa): Observable<ResultadoPesquisa> {
    return this.http.post<ResultadoPesquisa>(`${this.apiUrl}/pesquisa`, pedido);
  }
}
