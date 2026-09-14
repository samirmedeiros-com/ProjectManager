import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { SeurAuthService } from './seur-auth.service';
import { Pagina } from './contas.service';

/** O estado como o ecrã o lê. Na tabela são cinco letras: Y, N e três variantes de erro. */
export type EstadoPush = 'enviado' | 'erro' | 'pendente';

export interface PushResumo {
  hhpRowId: string;
  guia: string | null;
  lote: string | null;
  userLogin: string | null;
  conta: string | null;
  referencia: string | null;
  codigo: string | null;
  descricao: string | null;
  /** A letra original (Y/N/E/X/Z) — fica à vista porque distingue erros de clientes diferentes. */
  flag: string | null;
  estado: EstadoPush;
  criado: string | null;
  processado: string | null;
  destino: string | null;
}

export interface PushDetalhe extends PushResumo {
  pedido: string | null;
  resposta: string | null;
  utilizador: string | null;
  dataEvento: string | null;
  observacoes: string | null;
  motorista: string | null;
  pudo: string | null;
}

export interface PushEstatisticas {
  dia: string;
  enviados: number;
  erros: number;
  pendentes: number;
  total: number;
}

export interface PushPorUserLogin {
  userLogin: string;
  enviados: number;
  erros: number;
  pendentes: number;
  total: number;
}

export interface PushPorHora {
  hora: number;
  enviados: number;
  erros: number;
  pendentes: number;
  total: number;
}

export interface ResultadoReenvio {
  pedidos: number;
  repostos: number;
  naoEncontrados: string[];
}

export interface ReenvioLog {
  id: number;
  utilizador: string | null;
  hhpRowId: string;
  guia: string | null;
  userLogin: string | null;
  conta: string | null;
  flagAnterior: string | null;
  sucesso: boolean;
  mensagem: string | null;
  criadoEm: string;
}

export interface FiltroPesquisa {
  data?: string;
  userlogin?: string;
  conta?: string;
  guia?: string;
  estado?: string;
  pagina?: number;
  tamanho?: number;
}

@Injectable({ providedIn: 'root' })
export class TracePushService {
  private apiUrl = `${environment.seurApiUrl}/api/tracepush`;

  constructor(private http: HttpClient, private seurAuth: SeurAuthService) {}

  /** Mesmas credenciais da Gestão de Dados — ver o comentário no ContasService. */
  private h(): HttpHeaders {
    return new HttpHeaders({ Authorization: `Bearer ${this.seurAuth.getToken()}` });
  }

  estatisticas(data?: string): Observable<PushEstatisticas> {
    let params = new HttpParams();
    if (data) params = params.set('data', data);
    return this.http.get<PushEstatisticas>(`${this.apiUrl}/estatisticas`, { headers: this.h(), params });
  }

  porUserLogin(data?: string): Observable<PushPorUserLogin[]> {
    let params = new HttpParams();
    if (data) params = params.set('data', data);
    return this.http.get<PushPorUserLogin[]>(`${this.apiUrl}/por-userlogin`, { headers: this.h(), params });
  }

  porHora(userlogin: string, data?: string): Observable<PushPorHora[]> {
    let params = new HttpParams().set('userlogin', userlogin);
    if (data) params = params.set('data', data);
    return this.http.get<PushPorHora[]>(`${this.apiUrl}/por-hora`, { headers: this.h(), params });
  }

  /**
   * A data só vai quando não há número de guia: uma guia procura-se em toda a tabela, e
   * juntar-lhe a data faria desaparecer do resultado precisamente a guia que se procura
   * quando ela é de outro dia.
   */
  procurar(f: FiltroPesquisa): Observable<Pagina<PushResumo>> {
    let params = new HttpParams()
      .set('pagina', String(f.pagina ?? 1))
      .set('tamanho', String(f.tamanho ?? 10));

    if (f.guia) {
      params = params.set('guia', f.guia);
    } else {
      if (f.data) params = params.set('data', f.data);
      if (f.userlogin) params = params.set('userlogin', f.userlogin);
      if (f.conta) params = params.set('conta', f.conta);
      if (f.estado) params = params.set('estado', f.estado);
    }

    return this.http.get<Pagina<PushResumo>>(this.apiUrl, { headers: this.h(), params });
  }

  obter(hhpRowId: string): Observable<PushDetalhe> {
    return this.http.get<PushDetalhe>(`${this.apiUrl}/${encodeURIComponent(hhpRowId)}`, { headers: this.h() });
  }

  reenviar(hhpRowIds: string[]): Observable<ResultadoReenvio> {
    return this.http.post<ResultadoReenvio>(`${this.apiUrl}/reenviar`, { hhpRowIds }, { headers: this.h() });
  }

  logs(limite = 100, userlogin?: string, guia?: string): Observable<ReenvioLog[]> {
    let params = new HttpParams().set('limite', String(limite));
    if (userlogin) params = params.set('userlogin', userlogin);
    if (guia) params = params.set('guia', guia);
    return this.http.get<ReenvioLog[]>(`${this.apiUrl}/logs`, { headers: this.h(), params });
  }
}
