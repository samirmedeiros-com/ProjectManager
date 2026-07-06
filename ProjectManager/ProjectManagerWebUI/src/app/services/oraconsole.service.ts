import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { OraConsoleAuthService } from './oraconsole-auth.service';
import { OraConsoleCellUpdateRequest, OraConsoleColumn, OraConsoleQueryResult, OraConsoleSchema, OraConsoleTable } from '../models/oraconsole.model';

@Injectable({ providedIn: 'root' })
export class OraConsoleService {
  private apiUrl = `${environment.apiUrl}/api/oraconsole`;

  constructor(private http: HttpClient, private oraAuth: OraConsoleAuthService) {}

  private headers(): HttpHeaders {
    return new HttpHeaders({ Authorization: `Bearer ${this.oraAuth.getToken()}` });
  }

  getSchemas(): Observable<OraConsoleSchema[]> {
    return this.http.get<OraConsoleSchema[]>(`${this.apiUrl}/schemas`, { headers: this.headers() });
  }

  getTables(owner: string): Observable<OraConsoleTable[]> {
    return this.http.get<OraConsoleTable[]>(`${this.apiUrl}/schemas/${owner}/tables`, { headers: this.headers() });
  }

  getColumns(owner: string, tableName: string): Observable<OraConsoleColumn[]> {
    return this.http.get<OraConsoleColumn[]>(`${this.apiUrl}/schemas/${owner}/tables/${tableName}/columns`, { headers: this.headers() });
  }

  execute(sql: string, page: number = 1, pageSize: number = 100): Observable<OraConsoleQueryResult> {
    return this.http.post<OraConsoleQueryResult>(`${this.apiUrl}/query/execute`, { sql, page, pageSize }, { headers: this.headers() });
  }

  updateCell(request: OraConsoleCellUpdateRequest): Observable<OraConsoleQueryResult> {
    return this.http.put<OraConsoleQueryResult>(`${this.apiUrl}/cell`, request, { headers: this.headers() });
  }
}
