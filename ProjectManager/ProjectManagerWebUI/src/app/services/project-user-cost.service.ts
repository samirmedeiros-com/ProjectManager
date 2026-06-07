import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface ProjectUserCost {
  id: number;
  projectId: number;
  userId: number;
  userName: string;
  costPerHour: number;
}

export interface CreateProjectUserCostRequest {
  costPerHour: number;
}

@Injectable({
  providedIn: 'root'
})
export class ProjectUserCostService {
  private apiUrl = 'http://localhost:5000/api/projects';

  constructor(private http: HttpClient) {}

  getCostsByProject(projectId: number): Observable<ProjectUserCost[]> {
    return this.http.get<ProjectUserCost[]>(`${this.apiUrl}/${projectId}/user-costs`);
  }

  getCost(projectId: number, userId: number): Observable<ProjectUserCost> {
    return this.http.get<ProjectUserCost>(`${this.apiUrl}/${projectId}/user-costs/${userId}`);
  }

  createOrUpdateCost(projectId: number, userId: number, costPerHour: number): Observable<ProjectUserCost> {
    const request: CreateProjectUserCostRequest = { costPerHour };
    return this.http.post<ProjectUserCost>(`${this.apiUrl}/${projectId}/user-costs/${userId}`, request);
  }

  deleteCost(projectId: number, userId: number): Observable<any> {
    return this.http.delete(`${this.apiUrl}/${projectId}/user-costs/${userId}`);
  }
}
