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
        // Distinguir "não pertence ao setor" de "a API não respondeu": tratar tudo como 403
        // mostraria "reservado ao setor IT" a quem, na verdade, apanhou um 404 ou o backend em baixo.
        const motivo = err?.status === 403 ? 'semSetor' : 'indisponivel';
        this.router.navigate(['/portal'], { queryParams: { opensearch: motivo } });
        return of(false);
      }),
    );
  }
}
