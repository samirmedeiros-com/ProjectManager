import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { Router } from '@angular/router';

export const seurInterceptor: HttpInterceptorFn = (req, next) => {
  const router = inject(Router);

  if (!req.url.includes(':5001')) {
    return next(req);
  }

  return next(req).pipe(
    catchError((error: HttpErrorResponse) => {
      if (error.status === 401) {
        localStorage.removeItem('seur_token');
        localStorage.removeItem('seur_user');
        router.navigate(['/portal']);
      }
      return throwError(() => error);
    })
  );
};
