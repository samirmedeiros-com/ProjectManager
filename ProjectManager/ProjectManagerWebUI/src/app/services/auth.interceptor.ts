import { Injectable } from '@angular/core';
import { HttpRequest, HttpHandler, HttpEvent, HttpInterceptor, HttpErrorResponse } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { Router } from '@angular/router';

@Injectable()
export class AuthInterceptor implements HttpInterceptor {
  constructor(private router: Router) {}

  intercept(request: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
    const token = localStorage.getItem('token');

    if (token) {
      request = request.clone({
        setHeaders: {
          Authorization: `Bearer ${token}`
        }
      });
    }

    return next.handle(request).pipe(
      catchError((error: HttpErrorResponse) => {
        // Handle 401 Unauthorized (token expired or invalid)
        if (error.status === 401) {
          // Clear stored token and user data
          localStorage.removeItem('token');
          localStorage.removeItem('user');

          // Redirect to login page
          this.router.navigate(['/login']);

          // Show a message (optional - can be improved with a toast service)
          console.warn('Token expirado. Redirecionando para login...');
        }

        return throwError(() => error);
      })
    );
  }
}
