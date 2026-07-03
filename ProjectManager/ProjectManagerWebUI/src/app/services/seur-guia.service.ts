import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { SeurGuiaList, SeurGuiaDetail, SeurErro, SeurParcel, SeurVerify, SeurTotais } from '../models/seur-guia.model';
import { SeurAuthService } from './seur-auth.service';

@Injectable({ providedIn: 'root' })
export class SeurGuiaService {
  private apiUrl = `${environment.seurApiUrl}/api/seur/guias`;

  constructor(private http: HttpClient, private seurAuth: SeurAuthService) {}

  private headers(): HttpHeaders {
    return new HttpHeaders({ Authorization: `Bearer ${this.seurAuth.getToken()}` });
  }

  getGuias(guia?: string, referencia?: string, data?: string, flagAtlas?: string): Observable<SeurGuiaList[]> {
    let params = new HttpParams();
    if (guia) params = params.set('guia', guia);
    if (referencia) params = params.set('referencia', referencia);
    if (data) params = params.set('data', data);
    if (flagAtlas) params = params.set('flagAtlas', flagAtlas);
    return this.http.get<SeurGuiaList[]>(this.apiUrl, { headers: this.headers(), params });
  }

  getGuia(idt: number): Observable<SeurGuiaDetail> {
    return this.http.get<SeurGuiaDetail>(`${this.apiUrl}/${idt}`, { headers: this.headers() });
  }

  updateGuia(idt: number, data: Partial<SeurGuiaDetail>): Observable<any> {
    return this.http.put(`${this.apiUrl}/${idt}`, data, { headers: this.headers() });
  }

  updateFlagAtlas(idt: number, flagAtlas: string): Observable<any> {
    return this.http.put(`${this.apiUrl}/${idt}/flagatlas`, { flagAtlas }, { headers: this.headers() });
  }

  getErros(idt: number, referencia: string): Observable<SeurErro[]> {
    const params = new HttpParams().set('referencia', referencia);
    return this.http.get<SeurErro[]>(`${this.apiUrl}/${idt}/erros`, { headers: this.headers(), params });
  }

  getParcels(idt: number, guia: string): Observable<SeurParcel[]> {
    const params = new HttpParams().set('guia', guia);
    return this.http.get<SeurParcel[]>(`${this.apiUrl}/${idt}/parcels`, { headers: this.headers(), params });
  }

  getVerify(idt: number, guia: string): Observable<SeurVerify[]> {
    const params = new HttpParams().set('guia', guia);
    return this.http.get<SeurVerify[]>(`${this.apiUrl}/${idt}/verify`, { headers: this.headers(), params });
  }

  updateVerifyFlag(verifyIdt: number, verifyFlag: string): Observable<any> {
    return this.http.put(`${this.apiUrl}/verify/${verifyIdt}/flag`, { verifyFlag }, { headers: this.headers() });
  }

  getTotais(data: string): Observable<SeurTotais> {
    const params = new HttpParams().set('data', data);
    return this.http.get<SeurTotais>(`${this.apiUrl}/totais`, { headers: this.headers(), params });
  }
}
