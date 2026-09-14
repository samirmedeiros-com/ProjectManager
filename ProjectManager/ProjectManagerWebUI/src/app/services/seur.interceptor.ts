import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { Router } from '@angular/router';

/**
 * Onde reentrar depois de a sessão SEUR cair. As três aplicações que usam estas credenciais
 * têm ecrã de entrada próprio desde que os logins foram personalizados por app: mandar toda
 * a gente para o portal do Project Manager deixava quem estava na Gestão de Dados a olhar
 * para uma aplicação diferente daquela em que estava a trabalhar, sem perceber porquê.
 *
 * A ordem importa: /contas tem de ser testado antes de /opensearch só porque são caminhos
 * distintos, mas o prefixo é comparado por início e não por conter — /contas-x não é /contas.
 */
const LOGIN_POR_AREA: { prefixo: string; login: string }[] = [
  { prefixo: '/contas', login: '/login-contas' },
  { prefixo: '/opensearch', login: '/login-opensearch' },
  { prefixo: '/seur', login: '/login-seur' },
];

/**
 * Os endpoints que autenticam. O 401 que eles devolvem quer dizer "estas credenciais não
 * servem" e não "a sua sessão caiu" — tratá-los como sessão expirada apagava a mensagem
 * real ("Email ou password inválidos") e mandava a pessoa de volta ao ecrã de onde tinha
 * acabado de submeter, a ler que tinha expirado uma sessão que nunca chegou a existir.
 */
const ENDPOINTS_DE_ENTRADA = ['/auth/login', '/auth/forgot-password'];

export const seurInterceptor: HttpInterceptorFn = (req, next) => {
  const router = inject(Router);

  if (!req.url.includes(':5001')) {
    return next(req);
  }

  if (ENDPOINTS_DE_ENTRADA.some((e) => req.url.includes(e))) {
    return next(req);
  }

  return next(req).pipe(
    catchError((error: HttpErrorResponse) => {
      if (error.status === 401) {
        localStorage.removeItem('seur_token');
        localStorage.removeItem('seur_user');

        // O caminho actual, sem query string: é onde a pessoa estava, e é para onde deve voltar.
        const url = router.url.split('?')[0];
        const area = LOGIN_POR_AREA.find(
          (a) => url === a.prefixo || url.startsWith(a.prefixo + '/'),
        );

        if (area) {
          router.navigate([area.login], {
            queryParams: { sessao: 'expirada', returnUrl: area.prefixo },
          });
        } else {
          router.navigate(['/portal'], { queryParams: { sessao: 'expirada' } });
        }
      }
      return throwError(() => error);
    }),
  );
};
