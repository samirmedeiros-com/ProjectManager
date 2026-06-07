import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { Timesheet, TimesheetEntry, TimesheetListItem } from '../models/timesheet.model';

@Injectable({
  providedIn: 'root'
})
export class TimesheetService {
  private apiUrl = `${environment.apiUrl}/api/timesheets`;

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

  getDayTimesheet(date: Date): Observable<Timesheet> {
    const dateStr = date.toISOString().split('T')[0];
    return this.http.get<Timesheet>(`${this.apiUrl}/dia/${dateStr}`, { headers: this.getHeaders() });
  }

  getTimesheet(id: number): Observable<Timesheet> {
    return this.http.get<Timesheet>(`${this.apiUrl}/${id}`, { headers: this.getHeaders() });
  }

  getMyTimesheets(startDate?: Date, endDate?: Date): Observable<TimesheetListItem[]> {
    let url = `${this.apiUrl}/meus`;
    const params = [];
    if (startDate) params.push(`startDate=${startDate.toISOString().split('T')[0]}`);
    if (endDate) params.push(`endDate=${endDate.toISOString().split('T')[0]}`);
    if (params.length > 0) url += '?' + params.join('&');

    return this.http.get<TimesheetListItem[]>(url, { headers: this.getHeaders() });
  }

  addProjectEntry(timesheetId: number, projectId: number, workHours: number, notes?: string): Observable<Timesheet> {
    const request = { workHours, notes };
    return this.http.post<Timesheet>(`${this.apiUrl}/${timesheetId}/projeto/${projectId}`, request, { headers: this.getHeaders() });
  }

  removeProjectEntry(timesheetId: number, projectId: number): Observable<Timesheet> {
    return this.http.delete<Timesheet>(`${this.apiUrl}/${timesheetId}/projeto/${projectId}`, { headers: this.getHeaders() });
  }

  submitTimesheet(id: number): Observable<Timesheet> {
    return this.http.put<Timesheet>(`${this.apiUrl}/${id}/submeter`, {}, { headers: this.getHeaders() });
  }

  approveTimesheet(id: number): Observable<Timesheet> {
    return this.http.put<Timesheet>(`${this.apiUrl}/${id}/aprovar`, {}, { headers: this.getHeaders() });
  }

  rejectTimesheet(id: number, reason: string): Observable<Timesheet> {
    const request = { reason };
    return this.http.put<Timesheet>(`${this.apiUrl}/${id}/rejeitar`, request, { headers: this.getHeaders() });
  }

  deleteTimesheet(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`, { headers: this.getHeaders() });
  }

  getPendingApprovals(setorId: number): Observable<TimesheetListItem[]> {
    return this.http.get<TimesheetListItem[]>(`${this.apiUrl}/pendentes-aprovacao/${setorId}`, { headers: this.getHeaders() });
  }
}
