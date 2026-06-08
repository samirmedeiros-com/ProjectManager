import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Event, CreateEventRequest, UpdateEventRequest } from '../models/event.model';

@Injectable({
  providedIn: 'root'
})
export class EventService {
  private apiUrl = 'http://localhost:5000/api/events';

  constructor(private http: HttpClient) { }

  getUserEvents(startDate?: Date, endDate?: Date): Observable<Event[]> {
    let params = '';
    if (startDate) {
      params += `?startDate=${startDate.toISOString().split('T')[0]}`;
    }
    if (endDate) {
      params += (params ? '&' : '?') + `endDate=${endDate.toISOString().split('T')[0]}`;
    }
    return this.http.get<Event[]>(`${this.apiUrl}${params}`);
  }

  getEvent(id: number): Observable<Event> {
    return this.http.get<Event>(`${this.apiUrl}/${id}`);
  }

  createEvent(event: CreateEventRequest): Observable<Event> {
    return this.http.post<Event>(this.apiUrl, event);
  }

  updateEvent(id: number, event: UpdateEventRequest): Observable<Event> {
    return this.http.put<Event>(`${this.apiUrl}/${id}`, event);
  }

  deleteEvent(id: number, deleteAll: boolean = false): Observable<void> {
    let url = `${this.apiUrl}/${id}`;
    if (deleteAll) {
      url += '?deleteAll=true';
    }
    return this.http.delete<void>(url);
  }
}
