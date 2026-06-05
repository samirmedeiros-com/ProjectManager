import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { AuthService } from '../../services/auth.service';
import { environment } from '../../../environments/environment';

@Component({
  selector: 'app-debug',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="debug-container">
      <h1>🔧 Console de Debug</h1>

      <div class="section">
        <h2>Armazenamento de Token</h2>
        <button (click)="checkStorage()">Verificar Token</button>
        <pre>{{ storageInfo }}</pre>
      </div>

      <div class="section">
        <h2>Teste de Requisição</h2>
        <button (click)="testRequest()">GET /api/projects</button>
        <pre>{{ requestResult }}</pre>
      </div>

      <div class="section">
        <h2>Teste de Interceptor</h2>
        <button (click)="testInterceptor()">Testar Interceptor</button>
        <pre>{{ interceptorResult }}</pre>
      </div>
    </div>
  `,
  styles: [`
    .debug-container {
      padding: 20px;
      max-width: 800px;
      margin: 0 auto;
    }
    .section {
      margin: 20px 0;
      padding: 15px;
      border: 1px solid #ddd;
      border-radius: 4px;
    }
    button {
      padding: 10px 15px;
      background: #667eea;
      color: white;
      border: none;
      border-radius: 4px;
      cursor: pointer;
      margin-right: 10px;
    }
    button:hover {
      background: #764ba2;
    }
    pre {
      background: #f0f0f0;
      padding: 10px;
      border-radius: 4px;
      overflow-x: auto;
      font-size: 12px;
    }
  `]
})
export class DebugComponent implements OnInit {
  storageInfo = '';
  requestResult = '';
  interceptorResult = '';

  constructor(
    private authService: AuthService,
    private http: HttpClient
  ) { }

  ngOnInit() {
    this.checkStorage();
  }

  checkStorage() {
    const token = localStorage.getItem('token');
    const user = localStorage.getItem('user');

    this.storageInfo = `Token existe: ${!!token}\n`;
    this.storageInfo += `Token: ${token ? token.substring(0, 50) + '...' : 'Nenhum'}\n`;
    this.storageInfo += `Usuário: ${user || 'Nenhum'}\n`;
    this.storageInfo += `\nToken do AuthService: ${this.authService.getToken()?.substring(0, 50)}...\n`;
    this.storageInfo += `Autenticado: ${this.authService.isAuthenticated()}`;
  }

  testRequest() {
    this.requestResult = 'Testando...';
    this.http.get<any>(`${environment.apiUrl}/api/projects`)
      .subscribe(
        (data) => {
          this.requestResult = '✅ Sucesso!\n\n' + JSON.stringify(data, null, 2);
        },
        (error) => {
          this.requestResult = `❌ Erro: ${error.status} ${error.statusText}\n\n${JSON.stringify(error.error, null, 2)}`;
        }
      );
  }

  testInterceptor() {
    const token = this.authService.getToken();
    this.interceptorResult = `Token disponível: ${!!token}\n`;
    this.interceptorResult += `Valor do token: ${token?.substring(0, 50)}...\n`;
    this.interceptorResult += `\nTentando requisição com token...\n`;

    this.http.get<any>(`${environment.apiUrl}/api/projects`).subscribe(
      (data) => {
        this.interceptorResult += `✅ Interceptor funcionando! Obteve ${(data as any[]).length} projetos`;
      },
      (error) => {
        this.interceptorResult += `❌ Problema no interceptor: ${error.status}`;
      }
    );
  }
}
