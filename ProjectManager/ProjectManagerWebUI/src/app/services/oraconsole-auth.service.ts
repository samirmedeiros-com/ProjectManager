import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable, tap } from 'rxjs';
import { environment } from '../../environments/environment';
import { OraConsoleLoginRequest, OraConsoleLoginResponse } from '../models/oraconsole.model';

@Injectable({ providedIn: 'root' })
export class OraConsoleAuthService {
  private apiUrl = `${environment.apiUrl}/api/oraconsole/auth`;
  private currentUsernameSubject: BehaviorSubject<string | null>;
  public currentUsername: Observable<string | null>;

  private readonly TOKEN_KEY = 'oraconsole_token';
  private readonly USERNAME_KEY = 'oraconsole_username';

  constructor(private http: HttpClient) {
    this.currentUsernameSubject = new BehaviorSubject<string | null>(localStorage.getItem(this.USERNAME_KEY));
    this.currentUsername = this.currentUsernameSubject.asObservable();
  }

  get currentUsernameValue(): string | null {
    return this.currentUsernameSubject.value;
  }

  login(request: OraConsoleLoginRequest): Observable<OraConsoleLoginResponse> {
    return this.http.post<OraConsoleLoginResponse>(`${this.apiUrl}/login`, request).pipe(
      tap((response) => {
        if (response.success && response.token && response.username) {
          localStorage.setItem(this.TOKEN_KEY, response.token);
          localStorage.setItem(this.USERNAME_KEY, response.username);
          this.currentUsernameSubject.next(response.username);
        }
      })
    );
  }

  logout(): void {
    if (this.getToken()) {
      this.http.post(`${this.apiUrl}/logout`, {}, { headers: { Authorization: `Bearer ${this.getToken()}` } }).subscribe({
        error: () => {}
      });
    }
    localStorage.removeItem(this.TOKEN_KEY);
    localStorage.removeItem(this.USERNAME_KEY);
    this.currentUsernameSubject.next(null);
  }

  getToken(): string | null {
    return localStorage.getItem(this.TOKEN_KEY);
  }

  isAuthenticated(): boolean {
    return !!this.getToken();
  }
}
