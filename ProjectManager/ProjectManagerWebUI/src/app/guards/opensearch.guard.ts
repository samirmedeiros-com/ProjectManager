import { Injectable } from '@angular/core';
import { CanActivate, Router } from '@angular/router';
import { Observable, of } from 'rxjs';
import { catchError, map } from 'rxjs/operators';
import { SeurAuthService } from '../services/seur-auth.service';
import { OpenSearchService } from '../services/opensearch.service';

/**
 * A Consulta OpenSearch não tem login próprio: usa as credenciais da Gestão SEUR.
 * O token do SEUR e o do Project Manager são assinados com a mesma chave, por isso quem
 * separa as duas sessões é o claim "app" — confirmado no servidor pelo [RequerApp]. Este
 * guard é conveniência de navegação; quem garante o acesso é a API.
 */
@Injectable({ providedIn: 'root' })
export class OpenSearchGuard implements CanActivate {
  constructor(
    private router: Router,
    private seurAuth: SeurAuthService,
    private openSearch: OpenSearchService,
  ) {}

  canActivate(): Observable<boolean> {
    if (!this.seurAuth.isAuthenticated()) {
      this.router.navigate(['/login-seur'], { queryParams: { returnUrl: '/opensearch' } });
      return of(false);
    }

    return this.openSearch.acesso().pipe(
      map(() => true),
      catchError((err) => {
        // 401 aqui é sessão SEUR expirada ou token de outra aplicação do portal: em ambos
        // os casos o caminho é voltar a entrar na Gestão SEUR.
        if (err?.status === 401) {
          this.seurAuth.logout();
          this.router.navigate(['/login-seur'], { queryParams: { returnUrl: '/opensearch' } });
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
