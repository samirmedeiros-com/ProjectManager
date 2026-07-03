import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { SeurAuthService } from './seur-auth.service';
import {
  PagedResult,
  SeurCpostal, SaveCpostalDto,
  SeurDestino, SaveDestinoDto,
  SeurProduct, SaveProductDto,
  SeurService, SaveServiceDto,
  CwentNum, SaveCwentNumDto, CreateCwentNumDto
} from '../models/seur-tabelas.model';

@Injectable({ providedIn: 'root' })
export class SeurTabelasService {
  private base = `${environment.seurApiUrl}/api/seur/tabelas`;

  constructor(private http: HttpClient, private seurAuth: SeurAuthService) {}

  private h(): HttpHeaders {
    return new HttpHeaders({ Authorization: `Bearer ${this.seurAuth.getToken()}` });
  }

  // CPOSTAL
  getCpostais(search?: string, page = 1, pageSize = 100): Observable<PagedResult<SeurCpostal>> {
    let params = new HttpParams().set('page', page).set('pageSize', pageSize);
    if (search) params = params.set('search', search);
    return this.http.get<PagedResult<SeurCpostal>>(`${this.base}/cpostal`, { headers: this.h(), params });
  }
  createCpostal(dto: SaveCpostalDto): Observable<SeurCpostal> {
    return this.http.post<SeurCpostal>(`${this.base}/cpostal`, dto, { headers: this.h() });
  }
  updateCpostal(idt: number, dto: SaveCpostalDto): Observable<SeurCpostal> {
    return this.http.put<SeurCpostal>(`${this.base}/cpostal/${idt}`, dto, { headers: this.h() });
  }
  deleteCpostal(idt: number): Observable<any> {
    return this.http.delete(`${this.base}/cpostal/${idt}`, { headers: this.h() });
  }

  // DESTINOS
  getDestinos(search?: string, page = 1, pageSize = 100): Observable<PagedResult<SeurDestino>> {
    let params = new HttpParams().set('page', page).set('pageSize', pageSize);
    if (search) params = params.set('search', search);
    return this.http.get<PagedResult<SeurDestino>>(`${this.base}/destinos`, { headers: this.h(), params });
  }
  createDestino(dto: SaveDestinoDto): Observable<SeurDestino> {
    return this.http.post<SeurDestino>(`${this.base}/destinos`, dto, { headers: this.h() });
  }
  updateDestino(idt: number, dto: SaveDestinoDto): Observable<SeurDestino> {
    return this.http.put<SeurDestino>(`${this.base}/destinos/${idt}`, dto, { headers: this.h() });
  }
  deleteDestino(idt: number): Observable<any> {
    return this.http.delete(`${this.base}/destinos/${idt}`, { headers: this.h() });
  }

  // PRODUCTS
  getProducts(search?: string): Observable<SeurProduct[]> {
    let params = new HttpParams();
    if (search) params = params.set('search', search);
    return this.http.get<SeurProduct[]>(`${this.base}/products`, { headers: this.h(), params });
  }
  createProduct(dto: SaveProductDto): Observable<SeurProduct> {
    return this.http.post<SeurProduct>(`${this.base}/products`, dto, { headers: this.h() });
  }
  updateProduct(idt: number, dto: SaveProductDto): Observable<SeurProduct> {
    return this.http.put<SeurProduct>(`${this.base}/products/${idt}`, dto, { headers: this.h() });
  }
  deleteProduct(idt: number): Observable<any> {
    return this.http.delete(`${this.base}/products/${idt}`, { headers: this.h() });
  }

  // CWENT_NUM
  getCwents(search?: string, page = 1, pageSize = 100): Observable<PagedResult<CwentNum>> {
    let params = new HttpParams().set('page', page).set('pageSize', pageSize);
    if (search) params = params.set('search', search);
    return this.http.get<PagedResult<CwentNum>>(`${this.base}/cwent`, { headers: this.h(), params });
  }
  createCwent(dto: CreateCwentNumDto): Observable<CwentNum> {
    return this.http.post<CwentNum>(`${this.base}/cwent`, dto, { headers: this.h() });
  }
  updateCwent(account: string, dto: SaveCwentNumDto): Observable<CwentNum> {
    return this.http.put<CwentNum>(`${this.base}/cwent/${encodeURIComponent(account)}`, dto, { headers: this.h() });
  }
  deleteCwent(account: string): Observable<any> {
    return this.http.delete(`${this.base}/cwent/${encodeURIComponent(account)}`, { headers: this.h() });
  }

  // SERVICES
  getServices(search?: string): Observable<SeurService[]> {
    let params = new HttpParams();
    if (search) params = params.set('search', search);
    return this.http.get<SeurService[]>(`${this.base}/services`, { headers: this.h(), params });
  }
  createService(dto: SaveServiceDto): Observable<SeurService> {
    return this.http.post<SeurService>(`${this.base}/services`, dto, { headers: this.h() });
  }
  updateService(idt: number, dto: SaveServiceDto): Observable<SeurService> {
    return this.http.put<SeurService>(`${this.base}/services/${idt}`, dto, { headers: this.h() });
  }
  deleteService(idt: number): Observable<any> {
    return this.http.delete(`${this.base}/services/${idt}`, { headers: this.h() });
  }
}
