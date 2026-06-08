import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Setor } from '../models/setor.model';

@Injectable({
  providedIn: 'root'
})
export class SetorService {
  private apiUrl = `${environment.apiUrl}/api/setores';

  constructor(private http: HttpClient) { }

  private getHeaders(): HttpHeaders {
    const token = localStorage.getItem('token');
    return new HttpHeaders({
      'Content-Type': 'application/json',
      'Authorization': `Bearer ${token || ''}`
    });
  }

  getAllSetores(): Observable<Setor[]> {
    return this.http.get<Setor[]>(this.apiUrl, { headers: this.getHeaders() });
  }

  getSetorById(id: number): Observable<Setor> {
    return this.http.get<Setor>(`${this.apiUrl}/${id}`, { headers: this.getHeaders() });
  }

  createSetor(request: any): Observable<Setor> {
    return this.http.post<Setor>(this.apiUrl, request, { headers: this.getHeaders() });
  }

  updateSetor(id: number, request: any): Observable<Setor> {
    return this.http.put<Setor>(`${this.apiUrl}/${id}`, request, { headers: this.getHeaders() });
  }

  deleteSetor(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`, { headers: this.getHeaders() });
  }

  getUsersInSetor(setorId: number): Observable<any[]> {
    return this.http.get<any[]>(`${this.apiUrl}/${setorId}/users`, { headers: this.getHeaders() });
  }
}
