import { Injectable } from '@angular/core';
import { CanActivate, Router } from '@angular/router';
import { Observable, of } from 'rxjs';
import { catchError, map } from 'rxjs/operators';
import { AuthService } from '../services/auth.service';
import { OpenSearchService } from '../services/opensearch.service';

/**
 * O portal de OpenSearch usa o login do Project Manager, mas só está aberto ao setor IT.
 * A pertença ao setor não vai no JWT, por isso tem de ser confirmada no servidor —
 * este guard é conveniência de navegação; quem garante o acesso é o [RequerSetor] da API.
 */
@Injectable({ providedIn: 'root' })
export class OpenSearchGuard implements CanActivate {
  constructor(
    private router: Router,
    private authService: AuthService,
    private openSearch: OpenSearchService,
  ) {}

  canActivate(): Observable<boolean> {
    if (!this.authService.currentUserValue) {
      this.router.navigate(['/login'], { queryParams: { returnUrl: '/opensearch' } });
      return of(false);
    }

    return this.openSearch.acesso().pipe(
      map(() => true),
      catchError((err) => {
        // 401 é sessão expirada, não indisponibilidade: quem trata disso (e limpa o
        // localStorage) é o auth.interceptor. Navegar aqui competiria com ele e mostraria
        // "o serviço não respondeu" a quem apenas precisa de entrar outra vez.
        if (err?.status === 401) {
          return of(false);
        }

        if (err?.status === 403) {
          this.router.navigate(['/portal'], { queryParams: { opensearch: 'semSetor' } });
          return of(false);
        }

        // Qualquer outra falha fica registada na consola: a mensagem ao utilizador é
        // deliberadamente simples, mas sem isto não há como diagnosticar.
        console.error('Falha ao verificar o acesso ao portal de OpenSearch:', err);
        this.router.navigate(['/portal'], { queryParams: { opensearch: 'indisponivel' } });
        return of(false);
      }),
    );
  }
}
