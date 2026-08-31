import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface NamespaceInfo {
  nome: string;
  totalDeployments: number;
  deploymentsProntos: number;
  deploymentsParados: number;
}

export type EstadoDeployment = 'pronto' | 'degradado' | 'parado' | 'a-atualizar';

export interface DeploymentInfo {
  namespace: string;
  nome: string;
  imagem: string;
  replicasDesejadas: number;
  replicasProntas: number;
  replicasDisponiveis: number;
  replicasAtualizadas: number;
  estado: EstadoDeployment;
  replicasAntesDeParar?: number | null;
  criado?: string | null;
  ultimoReinicio?: string | null;
  titulo?: string | null;
}

export interface NotaDeployment {
  titulo?: string | null;
  memo?: string | null;
  atualizadoPor?: string | null;
  atualizadoPorNome?: string | null;
  atualizadoEm?: string | null;
}

export interface PodInfo {
  nome: string;
  estado: string;
  no: string;
  ip?: string | null;
  reinicios: number;
  pronto: boolean;
  totalContentores: number;
  contentoresProntos: number;
  criado?: string | null;
  motivo?: string | null;
  contentores: string[];
}

export interface LinhaLog {
  tempo?: string | null;
  texto: string;
}

export interface ResultadoLog {
  linhas: LinhaLog[];
  ultimo?: string | null;
}

export interface RegistoAuditoria {
  id: number;
  userEmail: string;
  userName?: string | null;
  acao: string;
  namespace?: string | null;
  deployment?: string | null;
  sucesso: boolean;
  detalhe?: string | null;
  valorAnterior?: string | null;
  valorNovo?: string | null;
  ipOrigem?: string | null;
  criadoEm: string;
}

export interface PaginaAuditoria {
  total: number;
  pagina: number;
  tamanho: number;
  registos: RegistoAuditoria[];
}

export interface ResultadoComando {
  mensagem: string;
  deployment: DeploymentInfo;
}

@Injectable({ providedIn: 'root' })
export class KubernetesService {
  private readonly apiUrl = `${environment.apiUrl}/api/kubernetes`;

  constructor(private http: HttpClient) {}

  namespaces(): Observable<NamespaceInfo[]> {
    return this.http.get<NamespaceInfo[]>(`${this.apiUrl}/namespaces`);
  }

  deployments(ns: string): Observable<DeploymentInfo[]> {
    return this.http.get<DeploymentInfo[]>(`${this.apiUrl}/namespaces/${encodeURIComponent(ns)}/deployments`);
  }

  pods(ns: string, deployment: string): Observable<PodInfo[]> {
    return this.http.get<PodInfo[]>(
      `${this.apiUrl}/namespaces/${encodeURIComponent(ns)}/deployments/${encodeURIComponent(deployment)}/pods`);
  }

  /**
   * Consola de um pod. Com `desde` (o `ultimo` da resposta anterior) o servidor devolve só as
   * linhas novas — sem isso, cada leitura repetiria o log inteiro.
   */
  log(ns: string, pod: string, opcoes: { contentor?: string; linhas?: number; desde?: string } = {}):
    Observable<ResultadoLog> {
    const params: string[] = [];
    if (opcoes.contentor) params.push(`contentor=${encodeURIComponent(opcoes.contentor)}`);
    if (opcoes.linhas) params.push(`linhas=${opcoes.linhas}`);
    if (opcoes.desde) params.push(`desde=${encodeURIComponent(opcoes.desde)}`);

    const query = params.length ? `?${params.join('&')}` : '';

    return this.http.get<ResultadoLog>(
      `${this.apiUrl}/namespaces/${encodeURIComponent(ns)}/pods/${encodeURIComponent(pod)}/log${query}`);
  }

  /**
   * Registo de ações, do mais recente para o mais antigo. Sem `deployment` é a vista global,
   * que a API reserva ao Admin.
   */
  auditoria(filtro: {
    ns?: string;
    deployment?: string;
    acao?: string;
    utilizador?: string;
    pagina?: number;
    tamanho?: number;
  }): Observable<PaginaAuditoria> {
    const params: string[] = [];
    if (filtro.ns) params.push(`ns=${encodeURIComponent(filtro.ns)}`);
    if (filtro.deployment) params.push(`deployment=${encodeURIComponent(filtro.deployment)}`);
    if (filtro.acao) params.push(`acao=${encodeURIComponent(filtro.acao)}`);
    if (filtro.utilizador) params.push(`utilizador=${encodeURIComponent(filtro.utilizador)}`);
    params.push(`pagina=${filtro.pagina ?? 0}`);
    params.push(`tamanho=${filtro.tamanho ?? 25}`);

    return this.http.get<PaginaAuditoria>(`${this.apiUrl}/auditoria?${params.join('&')}`);
  }

  nota(ns: string, deployment: string): Observable<NotaDeployment> {
    return this.http.get<NotaDeployment>(
      `${this.apiUrl}/namespaces/${encodeURIComponent(ns)}/deployments/${encodeURIComponent(deployment)}/nota`);
  }

  gravarNota(ns: string, deployment: string, nota: { titulo?: string; memo?: string }):
    Observable<NotaDeployment> {
    return this.http.put<NotaDeployment>(
      `${this.apiUrl}/namespaces/${encodeURIComponent(ns)}/deployments/${encodeURIComponent(deployment)}/nota`,
      nota);
  }

  parar(ns: string, deployment: string): Observable<ResultadoComando> {
    return this.comando(ns, deployment, 'parar');
  }

  arrancar(ns: string, deployment: string): Observable<ResultadoComando> {
    return this.comando(ns, deployment, 'arrancar');
  }

  reiniciar(ns: string, deployment: string): Observable<ResultadoComando> {
    return this.comando(ns, deployment, 'reiniciar');
  }

  private comando(ns: string, deployment: string, verbo: string): Observable<ResultadoComando> {
    return this.http.post<ResultadoComando>(
      `${this.apiUrl}/namespaces/${encodeURIComponent(ns)}/deployments/${encodeURIComponent(deployment)}/${verbo}`,
      {});
  }
}
