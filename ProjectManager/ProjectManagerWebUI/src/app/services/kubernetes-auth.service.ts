import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable, tap } from 'rxjs';
import { environment } from '../../environments/environment';

export interface K8sUser {
  id: number;
  email: string;
  fullName: string;
  role?: string;
}

export interface K8sLoginResponse {
  success: boolean;
  token?: string;
  message?: string;
  user?: K8sUser;
}

export interface K8sUserDetail extends K8sUser {
  isActive: boolean;
  createdAt: string;
  lastLoginAt?: string;
}

/**
 * Sessão da Gestão Kubernetes. Chaves de localStorage próprias (`k8s_token`/`k8s_user`):
 * a aplicação tem credenciais separadas e não pode partilhar a sessão do Project Manager,
 * do SEUR nem do OraConsole — sair de uma não faz sair das outras.
 */
@Injectable({ providedIn: 'root' })
export class KubernetesAuthService {
  static readonly TOKEN_KEY = 'k8s_token';
  static readonly USER_KEY = 'k8s_user';

  private readonly apiUrl = `${environment.apiUrl}/api/kubernetes/auth`;
  private readonly utilizadorAtual = new BehaviorSubject<K8sUser | null>(this.lerUtilizador());

  readonly currentUser = this.utilizadorAtual.asObservable();

  constructor(private http: HttpClient) {}

  get currentUserValue(): K8sUser | null {
    return this.utilizadorAtual.value;
  }

  get isAdmin(): boolean {
    return this.currentUserValue?.role === 'Admin';
  }

  /** Leitor entra e vê tudo, mas não executa comandos — o servidor recusa-lhos na mesma. */
  get podeExecutarComandos(): boolean {
    const papel = this.currentUserValue?.role;
    return papel === 'Admin' || papel === 'Operador';
  }

  login(email: string, password: string): Observable<K8sLoginResponse> {
    return this.http.post<K8sLoginResponse>(`${this.apiUrl}/login`, { email, password }).pipe(
      tap((resposta) => {
        if (resposta.success && resposta.token && resposta.user) {
          localStorage.setItem(KubernetesAuthService.TOKEN_KEY, resposta.token);
          localStorage.setItem(KubernetesAuthService.USER_KEY, JSON.stringify(resposta.user));
          this.utilizadorAtual.next(resposta.user);
        }
      }),
    );
  }

  logout(): void {
    localStorage.removeItem(KubernetesAuthService.TOKEN_KEY);
    localStorage.removeItem(KubernetesAuthService.USER_KEY);
    this.utilizadorAtual.next(null);
  }

  get token(): string | null {
    return localStorage.getItem(KubernetesAuthService.TOKEN_KEY);
  }

  isAuthenticated(): boolean {
    return !!this.token;
  }

  /** Confirma a sessão contra o servidor: o token pode ter expirado desde o último acesso. */
  acesso(): Observable<{ permitido: boolean; papel?: string; nome?: string }> {
    return this.http.get<{ permitido: boolean; papel?: string; nome?: string }>(`${this.apiUrl}/acesso`);
  }

  alterarPassword(currentPassword: string, newPassword: string): Observable<{ message: string }> {
    return this.http.put<{ message: string }>(`${this.apiUrl}/change-password`, { currentPassword, newPassword });
  }

  // ── Gestão de utilizadores (Admin) ──────────────────────────────────

  utilizadores(): Observable<K8sUserDetail[]> {
    return this.http.get<K8sUserDetail[]>(`${this.apiUrl}/users`);
  }

  criarUtilizador(email: string, fullName: string, role: string):
    Observable<{ user: K8sUserDetail; tempPassword: string; emailSent: boolean }> {
    return this.http.post<{ user: K8sUserDetail; tempPassword: string; emailSent: boolean }>(
      `${this.apiUrl}/users`, { email, fullName, role });
  }

  desativarUtilizador(id: number): Observable<{ message: string }> {
    return this.http.delete<{ message: string }>(`${this.apiUrl}/users/${id}`);
  }

  /** Apaga a conta de vez. O histórico de ações mantém-se — guarda o nome e o email na linha. */
  removerUtilizador(id: number): Observable<{ message: string }> {
    return this.http.delete<{ message: string }>(`${this.apiUrl}/users/${id}/definitivo`);
  }

  reporPassword(id: number): Observable<{ tempPassword: string; emailSent: boolean }> {
    return this.http.post<{ tempPassword: string; emailSent: boolean }>(
      `${this.apiUrl}/users/${id}/reset-password`, {});
  }

  private lerUtilizador(): K8sUser | null {
    const guardado = localStorage.getItem(KubernetesAuthService.USER_KEY);
    return guardado ? JSON.parse(guardado) : null;
  }
}
