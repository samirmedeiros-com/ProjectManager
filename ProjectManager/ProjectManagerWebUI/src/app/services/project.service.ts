import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Project, CreateProjectRequest, UpdateProjectRequest } from '../models/project.model';
import { ProjectMember, AddProjectMemberRequest } from '../models/project-member.model';
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class ProjectService {
  private apiUrl = `${environment.apiUrl}/api/projects`;

  constructor(private http: HttpClient) { }

  private getHeaders(): HttpHeaders {
    const token = localStorage.getItem('token');
    console.log('Token from localStorage:', token ? 'EXISTS' : 'MISSING');
    if (token) {
      console.log('Token length:', token.length);
      console.log('Token starts with:', token.substring(0, 50) + '...');
      return new HttpHeaders({
        'Authorization': `Bearer ${token}`,
        'Content-Type': 'application/json'
      });
    }
    console.log('WARNING: No token found in localStorage!');
    return new HttpHeaders({
      'Content-Type': 'application/json'
    });
  }

  getAll(): Observable<Project[]> {
    return this.http.get<Project[]>(this.apiUrl, { headers: this.getHeaders() });
  }

  getById(id: number): Observable<Project> {
    return this.http.get<Project>(`${this.apiUrl}/${id}`, { headers: this.getHeaders() });
  }

  create(request: CreateProjectRequest): Observable<Project> {
    return this.http.post<Project>(this.apiUrl, request, { headers: this.getHeaders() });
  }

  update(id: number, request: UpdateProjectRequest): Observable<Project> {
    return this.http.put<Project>(`${this.apiUrl}/${id}`, request, { headers: this.getHeaders() });
  }

  updateManager(id: number, manager: string): Observable<Project> {
    return this.http.put<Project>(`${this.apiUrl}/${id}/manager`, { manager }, { headers: this.getHeaders() });
  }

  updateStatus(id: number, status: string): Observable<Project> {
    return this.http.put<Project>(`${this.apiUrl}/${id}/status`, { status }, { headers: this.getHeaders() });
  }

  updateOwner(id: number, ownerId: number | null): Observable<Project> {
    return this.http.put<Project>(`${this.apiUrl}/${id}/owner`, { ownerId }, { headers: this.getHeaders() });
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`, { headers: this.getHeaders() });
  }

  getMembers(projectId: number): Observable<ProjectMember[]> {
    return this.http.get<ProjectMember[]>(`${this.apiUrl}/${projectId}/members`, { headers: this.getHeaders() });
  }

  addMember(projectId: number, request: AddProjectMemberRequest): Observable<any> {
    return this.http.post(`${this.apiUrl}/${projectId}/members`, request, { headers: this.getHeaders() });
  }

  removeMember(memberId: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/members/${memberId}`, { headers: this.getHeaders() });
  }

  getComments(projectId: number): Observable<any[]> {
    return this.http.get<any[]>(`${this.apiUrl}/${projectId}/comments`, { headers: this.getHeaders() });
  }

  addComment(projectId: number, content: string): Observable<any> {
    return this.http.post(`${this.apiUrl}/${projectId}/comments`, { content }, { headers: this.getHeaders() });
  }

  getHistory(projectId: number): Observable<any[]> {
    return this.http.get<any[]>(`${this.apiUrl}/${projectId}/history`, { headers: this.getHeaders() });
  }

  getUserSectors(): Observable<any[]> {
    console.log('[ProjectService] Chamando /api/setor/user-available');
    const url = `${environment.apiUrl}/api/setor/user-available`;
    console.log('[ProjectService] URL:', url);
    return this.http.get<any[]>(url, { headers: this.getHeaders() });
  }
}
