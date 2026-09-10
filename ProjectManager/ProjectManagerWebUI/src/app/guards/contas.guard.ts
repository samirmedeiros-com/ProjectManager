import { Injectable } from '@angular/core';
import { CanActivate, Router } from '@angular/router';
import { Observable, of } from 'rxjs';
import { catchError, map } from 'rxjs/operators';
import { SeurAuthService } from '../services/seur-auth.service';
import { ContasService } from '../services/contas.service';

/**
 * A Gestão de Dados não tem login próprio: usa as credenciais da Gestão SEUR. O token do
 * SEUR e o do Project Manager são assinados com a mesma chave, por isso quem separa as duas
 * sessões é o claim "app" — confirmado no servidor pelo [RequerApp]. Este guard é
 * conveniência de navegação; quem garante o acesso é a API.
 */
@Injectable({ providedIn: 'root' })
export class ContasGuard implements CanActivate {
  constructor(
    private router: Router,
    private seurAuth: SeurAuthService,
    private contas: ContasService,
  ) {}

  canActivate(): Observable<boolean> {
    if (!this.seurAuth.isAuthenticated()) {
      this.router.navigate(['/login-contas'], { queryParams: { returnUrl: '/contas' } });
      return of(false);
    }

    return this.contas.acesso().pipe(
      map(() => true),
      catchError((err) => {
        // 401 aqui é sessão SEUR expirada ou token de outra aplicação do portal: em ambos
        // os casos o caminho é voltar a entrar na Gestão SEUR.
        if (err?.status === 401) {
          this.seurAuth.logout();
          this.router.navigate(['/login-contas'], { queryParams: { returnUrl: '/contas' } });
          return of(false);
        }

        console.error('Falha ao verificar o acesso à Gestão de Dados:', err);
        this.router.navigate(['/portal'], { queryParams: { contas: 'indisponivel' } });
        return of(false);
      }),
    );
  }
}
