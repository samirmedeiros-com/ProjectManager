import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';
import { RegisterRequest, RegisterResponse, User, UpdateUserRequest } from '../models/user.model';
import { Setor } from '../models/setor.model';
import { AuthService } from './auth.service';
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class UserService {
  private authApiUrl = `${environment.apiUrl}/api/auth`;
  private usersApiUrl = `${environment.apiUrl}/api/users`;

  constructor(private http: HttpClient, private authService: AuthService) {}

  private getHeaders(): HttpHeaders {
    const token = localStorage.getItem('token');
    return new HttpHeaders({
      'Content-Type': 'application/json',
      'Authorization': `Bearer ${token || ''}`
    });
  }

  registerUser(request: RegisterRequest): Observable<RegisterResponse> {
    return this.http.post<RegisterResponse>(
      `${this.authApiUrl}/register`,
      request,
      { headers: this.getHeaders() }
    );
  }

  getAllUsers(): Observable<User[]> {
    return this.http.get<User[]>(
      `${this.usersApiUrl}`,
      { headers: this.getHeaders() }
    );
  }

  getAllUsersUnfiltered(): Observable<User[]> {
    return this.http.get<User[]>(
      `${this.usersApiUrl}/all`,
      { headers: this.getHeaders() }
    );
  }

  getUserById(id: number): Observable<User> {
    return this.http.get<User>(
      `${this.usersApiUrl}/${id}`,
      { headers: this.getHeaders() }
    );
  }

  updateUser(id: number, request: UpdateUserRequest): Observable<User> {
    return this.http.put<User>(
      `${this.usersApiUrl}/${id}`,
      request,
      { headers: this.getHeaders() }
    );
  }

  deleteUser(id: number): Observable<void> {
    return this.http.delete<void>(
      `${this.usersApiUrl}/${id}`,
      { headers: this.getHeaders() }
    );
  }

  deactivateUser(id: number): Observable<User> {
    return this.http.patch<User>(
      `${this.usersApiUrl}/${id}/deactivate`,
      {},
      { headers: this.getHeaders() }
    );
  }

  activateUser(id: number): Observable<User> {
    return this.http.patch<User>(
      `${this.usersApiUrl}/${id}/activate`,
      {},
      { headers: this.getHeaders() }
    );
  }

  assignSetores(userId: number, setorIds: number[]): Observable<User> {
    return this.http.post<User>(
      `${this.usersApiUrl}/${userId}/setores`,
      { setorIds },
      { headers: this.getHeaders() }
    );
  }


  removeSetorFromUser(userId: number, setorId: number): Observable<void> {
    return this.http.delete<void>(
      `${this.usersApiUrl}/${userId}/setores/${setorId}`,
      { headers: this.getHeaders() }
    );
  }

  getCurrentUserSetores(): Observable<any[]> {
    const currentUser = this.authService.currentUserValue;
    if (!currentUser) {
      return new Observable(observer => {
        observer.next([]);
        observer.complete();
      });
    }
    return this.getUserSetores(currentUser.id);
  }

  private getUserSetores(userId: number): Observable<any[]> {
    return this.http.get<any[]>(
      `${this.usersApiUrl}/${userId}/setores`,
      { headers: this.getHeaders() }
    );
  }
}
