import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';
import { ProjectTask, CreateTaskRequest, UpdateTaskRequest } from '../models/task.model';
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class TaskService {
  private apiUrl = `${environment.apiUrl}/api/projects`;

  constructor(private http: HttpClient) { }

  private getHeaders(): HttpHeaders {
    const token = localStorage.getItem('token');
    if (token) {
      return new HttpHeaders({
        'Authorization': `Bearer ${token}`,
        'Content-Type': 'application/json'
      });
    }
    return new HttpHeaders({
      'Content-Type': 'application/json'
    });
  }

  getByProject(projectId: number): Observable<ProjectTask[]> {
    return this.http.get<ProjectTask[]>(`${this.apiUrl}/${projectId}/tasks`, { headers: this.getHeaders() });
  }

  getById(projectId: number, taskId: number): Observable<ProjectTask> {
    return this.http.get<ProjectTask>(`${this.apiUrl}/${projectId}/tasks/${taskId}`, { headers: this.getHeaders() });
  }

  create(projectId: number, request: CreateTaskRequest): Observable<ProjectTask> {
    return this.http.post<ProjectTask>(`${this.apiUrl}/${projectId}/tasks`, request, { headers: this.getHeaders() });
  }

  update(projectId: number, taskId: number, request: UpdateTaskRequest): Observable<ProjectTask> {
    return this.http.put<ProjectTask>(`${this.apiUrl}/${projectId}/tasks/${taskId}`, request, { headers: this.getHeaders() });
  }

  delete(projectId: number, taskId: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${projectId}/tasks/${taskId}`, { headers: this.getHeaders() });
  }
}
