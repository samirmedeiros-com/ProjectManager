import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { SeurAuthService } from './seur-auth.service';

export interface TaskConector {
  id: number;
  estado: string;
  worker: string | null;
  /** Stack trace de uma task falhada. Só vem no detalhe de um conector. */
  traco: string | null;
  emErro: boolean;
}

export interface Conector {
  nome: string;
  /** "source" (AS400 → tópico) ou "sink" (tópico → Oracle). */
  tipo: string;
  estado: string;
  worker: string | null;
  tasks: TaskConector[];
  topico: string | null;
  tabela: string | null;
  classe: string | null;
  emErro: boolean;
  tasksEmErro: number;
}

export interface ResumoKafka {
  total: number;
  aFuncionar: number;
  comErro: number;
  emPausa: number;
  conectores: Conector[];
}

export interface ResultadoReinicio {
  conector: string;
  task: number;
  sucesso: boolean;
  mensagem: string | null;
}

@Injectable({ providedIn: 'root' })
export class KafkaService {
  private apiUrl = `${environment.seurApiUrl}/api/kafka`;

  constructor(private http: HttpClient, private seurAuth: SeurAuthService) {}

  /** Mesmas credenciais da Gestão de Dados — ver o comentário no ContasService. */
  private h(): HttpHeaders {
    return new HttpHeaders({ Authorization: `Bearer ${this.seurAuth.getToken()}` });
  }

  conectores(): Observable<ResumoKafka> {
    return this.http.get<ResumoKafka>(`${this.apiUrl}/conectores`, { headers: this.h() });
  }

  conector(nome: string): Observable<Conector> {
    return this.http.get<Conector>(`${this.apiUrl}/conectores/${encodeURIComponent(nome)}`, { headers: this.h() });
  }

  reiniciarTask(nome: string, task: number): Observable<ResultadoReinicio> {
    return this.http.post<ResultadoReinicio>(
      `${this.apiUrl}/conectores/${encodeURIComponent(nome)}/tasks/${task}/reiniciar`,
      null,
      { headers: this.h() },
    );
  }
}
