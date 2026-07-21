import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { Router } from '@angular/router';

export const oraConsoleInterceptor: HttpInterceptorFn = (req, next) => {
  const router = inject(Router);

  if (!req.url.includes('/api/oraconsole/')) {
    return next(req);
  }

  return next(req).pipe(
    catchError((error: HttpErrorResponse) => {
      if (error.status === 401) {
        localStorage.removeItem('oraconsole_token');
        localStorage.removeItem('oraconsole_username');
        router.navigate(['/portal'], { queryParams: { sessao: 'expirada' } });
      }
      return throwError(() => error);
    })
  );
};
