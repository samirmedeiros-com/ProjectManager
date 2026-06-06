import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { Timesheet, TimesheetEntry, TimesheetListItem } from '../models/timesheet.model';

@Injectable({
  providedIn: 'root'
})
export class TimesheetService {
  private apiUrl = `${environment.apiUrl}/timesheets`;

  constructor(private http: HttpClient) { }

  createTimesheet(projectId: number, userId: number, weekStartDate: Date): Observable<Timesheet> {
    const request = {
      projectId,
      userId,
      weekStartDate
    };
    return this.http.post<Timesheet>(`${this.apiUrl}/criar`, request);
  }

  getTimesheet(id: number): Observable<Timesheet> {
    return this.http.get<Timesheet>(`${this.apiUrl}/${id}`);
  }

  getMyTimesheets(): Observable<TimesheetListItem[]> {
    return this.http.get<TimesheetListItem[]>(`${this.apiUrl}/meus-timesheets`);
  }

  getTimesheetsByProject(projectId: number): Observable<TimesheetListItem[]> {
    return this.http.get<TimesheetListItem[]>(`${this.apiUrl}/projeto/${projectId}`);
  }

  updateEntries(id: number, entries: TimesheetEntry[]): Observable<Timesheet> {
    const request = { entries };
    return this.http.put<Timesheet>(`${this.apiUrl}/${id}/horas`, request);
  }

  submitTimesheet(id: number): Observable<Timesheet> {
    return this.http.put<Timesheet>(`${this.apiUrl}/${id}/submeter`, {});
  }

  approveTimesheet(id: number): Observable<Timesheet> {
    return this.http.put<Timesheet>(`${this.apiUrl}/${id}/aprovar`, {});
  }

  rejectTimesheet(id: number, reason: string): Observable<Timesheet> {
    const request = { reason };
    return this.http.put<Timesheet>(`${this.apiUrl}/${id}/rejeitar`, request);
  }

  getPendingApprovals(setorId: number): Observable<TimesheetListItem[]> {
    return this.http.get<TimesheetListItem[]>(`${this.apiUrl}/pendentes-aprovacao/${setorId}`);
  }
}
