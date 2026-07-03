import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { SeurUserDetail, CreateSeurUserDto, CreateUserResponse, ResetPasswordResponse } from '../models/seur-user.model';
import { SeurAuthService } from './seur-auth.service';

@Injectable({ providedIn: 'root' })
export class SeurUsersService {
  private apiUrl = `${environment.seurApiUrl}/api/seur/auth`;

  constructor(private http: HttpClient, private seurAuth: SeurAuthService) {}

  private headers(): HttpHeaders {
    return new HttpHeaders({ Authorization: `Bearer ${this.seurAuth.getToken()}` });
  }

  getUsers(): Observable<SeurUserDetail[]> {
    return this.http.get<SeurUserDetail[]>(`${this.apiUrl}/users`, { headers: this.headers() });
  }

  createUser(dto: CreateSeurUserDto): Observable<CreateUserResponse> {
    return this.http.post<CreateUserResponse>(`${this.apiUrl}/users`, dto, { headers: this.headers() });
  }

  deactivateUser(id: number): Observable<any> {
    return this.http.delete(`${this.apiUrl}/users/${id}`, { headers: this.headers() });
  }

  resetPassword(id: number): Observable<ResetPasswordResponse> {
    return this.http.post<ResetPasswordResponse>(`${this.apiUrl}/users/${id}/reset-password`, {}, { headers: this.headers() });
  }

  changePassword(currentPassword: string, newPassword: string): Observable<any> {
    return this.http.put(`${this.apiUrl}/change-password`, { currentPassword, newPassword }, { headers: this.headers() });
  }
}
