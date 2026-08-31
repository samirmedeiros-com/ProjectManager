import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';
import { KubernetesAuthService } from './kubernetes-auth.service';

/**
 * Injeta o token da Gestão Kubernetes e trata o 401 desta aplicação.
 *
 * O `auth.interceptor` exclui `/api/kubernetes/` justamente para não pôr aqui o token do
 * Project Manager: um token válido de outra aplicação passaria o [Authorize] e só seria
 * recusado pelo claim "app" — o pedido chegaria ao servidor com a credencial errada.
 */
export const kubernetesInterceptor: HttpInterceptorFn = (req, next) => {
  if (!req.url.includes('/api/kubernetes/')) {
    return next(req);
  }

  const router = inject(Router);
  const token = localStorage.getItem(KubernetesAuthService.TOKEN_KEY);

  const pedido = token
    ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } })
    : req;

  return next(pedido).pipe(
    catchError((erro: HttpErrorResponse) => {
      // O login também responde 401 (credenciais erradas): redirecionar aí mandaria o
      // utilizador para o ecrã onde já está e apagaria a mensagem de erro.
      if (erro.status === 401 && !req.url.includes('/auth/login')) {
        localStorage.removeItem(KubernetesAuthService.TOKEN_KEY);
        localStorage.removeItem(KubernetesAuthService.USER_KEY);
        router.navigate(['/login-kubernetes'], { queryParams: { sessao: 'expirada' } });
      }
      return throwError(() => erro);
    }),
  );
};
