import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable, tap } from 'rxjs';
import { environment } from '../../environments/environment';

export interface SeurUser {
  id: number;
  email: string;
  fullName: string;
  role?: string;
}

export interface SeurLoginRequest {
  email: string;
  password: string;
}

export interface SeurLoginResponse {
  success: boolean;
  token?: string;
  message?: string;
  user?: SeurUser;
}

@Injectable({ providedIn: 'root' })
export class SeurAuthService {
  private apiUrl = `${environment.seurApiUrl}/api/seur/auth`;
  private currentUserSubject: BehaviorSubject<SeurUser | null>;
  public currentUser: Observable<SeurUser | null>;

  private readonly TOKEN_KEY = 'seur_token';
  private readonly USER_KEY = 'seur_user';

  constructor(private http: HttpClient) {
    this.currentUserSubject = new BehaviorSubject<SeurUser | null>(this.getUserFromStorage());
    this.currentUser = this.currentUserSubject.asObservable();
  }

  get currentUserValue(): SeurUser | null {
    return this.currentUserSubject.value;
  }

  login(request: SeurLoginRequest): Observable<SeurLoginResponse> {
    return this.http.post<SeurLoginResponse>(`${this.apiUrl}/login`, request).pipe(
      tap((response) => {
        if (response.success && response.token && response.user) {
          localStorage.setItem(this.TOKEN_KEY, response.token);
          localStorage.setItem(this.USER_KEY, JSON.stringify(response.user));
          this.currentUserSubject.next(response.user);
        }
      })
    );
  }

  logout(): void {
    localStorage.removeItem(this.TOKEN_KEY);
    localStorage.removeItem(this.USER_KEY);
    this.currentUserSubject.next(null);
  }

  getToken(): string | null {
    return localStorage.getItem(this.TOKEN_KEY);
  }

  isAuthenticated(): boolean {
    return !!this.getToken();
  }

  isAdmin(): boolean {
    return this.currentUserValue?.role === 'Admin';
  }

  forgotPassword(email: string): Observable<{ success: boolean; message: string }> {
    return this.http.post<{ success: boolean; message: string }>(
      `${this.apiUrl}/forgot-password`, { email, password: '' }
    );
  }

  private getUserFromStorage(): SeurUser | null {
    const user = localStorage.getItem(this.USER_KEY);
    return user ? JSON.parse(user) : null;
  }
}
