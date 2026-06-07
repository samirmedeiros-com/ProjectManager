import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface HoursByMonth {
  year: number;
  month: number;
  totalHours: number;
}

export interface HoursByProject {
  projectId: number;
  projectName: string;
  totalHours: number;
}

export interface HoursByUser {
  userId: number;
  userName: string;
  totalHours: number;
}

export interface ReportSummary {
  totalHours: number;
  totalTimesheets: number;
  uniqueUsers: number;
  uniqueProjects: number;
  hoursByMonth: HoursByMonth[];
  hoursByProject: HoursByProject[];
  hoursByUser: HoursByUser[];
}

@Injectable({
  providedIn: 'root'
})
export class ReportService {
  private apiUrl = 'http://localhost:5000/api/reports';

  constructor(private http: HttpClient) {}

  getSummary(monthOffset?: number): Observable<ReportSummary> {
    let url = `${this.apiUrl}/summary`;
    if (monthOffset) {
      url += `?monthOffset=${monthOffset}`;
    }
    return this.http.get<ReportSummary>(url);
  }

  getHoursByMonth(userId?: number): Observable<HoursByMonth[]> {
    let url = `${this.apiUrl}/hours-by-month`;
    if (userId) {
      url += `?userId=${userId}`;
    }
    return this.http.get<HoursByMonth[]>(url);
  }

  getHoursByProject(userId?: number, monthOffset?: number): Observable<HoursByProject[]> {
    let url = `${this.apiUrl}/hours-by-project`;
    const params = [];
    if (userId) params.push(`userId=${userId}`);
    if (monthOffset) params.push(`monthOffset=${monthOffset}`);
    if (params.length > 0) {
      url += `?${params.join('&')}`;
    }
    return this.http.get<HoursByProject[]>(url);
  }

  getHoursByUser(monthOffset?: number): Observable<HoursByUser[]> {
    let url = `${this.apiUrl}/hours-by-user`;
    if (monthOffset) {
      url += `?monthOffset=${monthOffset}`;
    }
    return this.http.get<HoursByUser[]>(url);
  }
}
