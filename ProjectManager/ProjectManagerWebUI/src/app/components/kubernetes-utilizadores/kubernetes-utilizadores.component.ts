import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { KubernetesAuthService, K8sUserDetail } from '../../services/kubernetes-auth.service';
import { KubernetesMenuComponent } from '../kubernetes-shared/kubernetes-menu.component';

@Component({
  selector: 'app-kubernetes-utilizadores',
  standalone: true,
  imports: [CommonModule, FormsModule, KubernetesMenuComponent],
  templateUrl: './kubernetes-utilizadores.component.html',
  styleUrls: ['./kubernetes-utilizadores.component.css'],
})
export class KubernetesUtilizadoresComponent implements OnInit {
  utilizadores: K8sUserDetail[] = [];
  aCarregar = false;
  erro = '';

  criando = false;
  novo = { fullName: '', email: '', role: 'Leitor' };
  aGravar = false;

  /**
   * Mostrada depois de criar ou repor: se o email falhar, esta é a única forma de o
   * administrador entregar a password.
   */
  resultado: { titulo: string; email: string; password: string; emailEnviado: boolean } | null = null;

  confirmacao: {
    utilizador: K8sUserDetail;
    titulo: string;
    texto: string;
    acao: 'desativar' | 'repor' | 'remover';
  } | null = null;

  readonly papeis = [
    { valor: 'Leitor', descricao: 'Consulta o cluster; não executa comandos' },
    { valor: 'Operador', descricao: 'Consulta e executa parar, arrancar e reiniciar' },
    { valor: 'Admin', descricao: 'Tudo, incluindo gerir utilizadores e ver o registo global' },
  ];

  constructor(private auth: KubernetesAuthService, private cdr: ChangeDetectorRef) {}

  ngOnInit(): void {
    this.carregar();
  }

  carregar(): void {
    this.aCarregar = true;
    this.erro = '';

    this.auth.utilizadores().subscribe({
      next: (lista) => {
        this.utilizadores = lista;
        this.aCarregar = false;
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.aCarregar = false;
        this.erro = this.explicar(err, 'Não foi possível ler os utilizadores.');
        this.cdr.detectChanges();
      },
    });
  }

  abrirNovo(): void {
    this.novo = { fullName: '', email: '', role: 'Leitor' };
    this.erro = '';
    this.criando = true;
  }

  cancelarNovo(): void {
    this.criando = false;
  }

  criar(): void {
    const nome = this.novo.fullName.trim();
    const email = this.novo.email.trim();

    if (!nome || !email.includes('@')) {
      this.erro = 'Indique o nome e um email válido.';
      return;
    }

    this.aGravar = true;
    this.erro = '';

    this.auth.criarUtilizador(email, nome, this.novo.role).subscribe({
      next: (r) => {
        this.aGravar = false;
        this.criando = false;
        this.resultado = {
          titulo: 'Utilizador criado',
          email: r.user.email,
          password: r.tempPassword,
          emailEnviado: r.emailSent,
        };
        this.carregar();
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.aGravar = false;
        this.erro = this.explicar(err, 'Não foi possível criar o utilizador.');
        this.cdr.detectChanges();
      },
    });
  }

  pedirRepor(u: K8sUserDetail): void {
    this.confirmacao = {
      utilizador: u,
      acao: 'repor',
      titulo: `Enviar nova password a ${u.fullName}?`,
      texto: `É gerada uma password nova e enviada para ${u.email}. A password atual deixa de funcionar.`,
    };
  }

  pedirDesativar(u: K8sUserDetail): void {
    this.confirmacao = {
      utilizador: u,
      acao: 'desativar',
      titulo: `Desativar ${u.fullName}?`,
      texto: 'Deixa de conseguir entrar. O histórico de ações mantém-se.',
    };
  }

  pedirRemover(u: K8sUserDetail): void {
    this.confirmacao = {
      utilizador: u,
      acao: 'remover',
      titulo: `Remover ${u.fullName}?`,
      texto:
        'A conta é apagada e não pode ser recuperada. O que este utilizador fez continua no ' +
        'registo de ações, com o nome e o email.',
    };
  }

  cancelarConfirmacao(): void {
    this.confirmacao = null;
  }

  confirmar(): void {
    const pedido = this.confirmacao;
    if (!pedido) return;

    this.confirmacao = null;
    this.erro = '';

    if (pedido.acao === 'repor') {
      this.auth.reporPassword(pedido.utilizador.id).subscribe({
        next: (r) => {
          this.resultado = {
            titulo: 'Password reposta',
            email: pedido.utilizador.email,
            password: r.tempPassword,
            emailEnviado: r.emailSent,
          };
          this.cdr.detectChanges();
        },
        error: (err) => {
          this.erro = this.explicar(err, 'Não foi possível repor a password.');
          this.cdr.detectChanges();
        },
      });
      return;
    }

    if (pedido.acao === 'remover') {
      this.auth.removerUtilizador(pedido.utilizador.id).subscribe({
        next: () => this.carregar(),
        error: (err) => {
          this.erro = this.explicar(err, 'Não foi possível remover o utilizador.');
          this.cdr.detectChanges();
        },
      });
      return;
    }

    this.auth.desativarUtilizador(pedido.utilizador.id).subscribe({
      next: () => this.carregar(),
      error: (err) => {
        this.erro = this.explicar(err, 'Não foi possível desativar o utilizador.');
        this.cdr.detectChanges();
      },
    });
  }

  fecharResultado(): void {
    this.resultado = null;
  }

  descricaoPapel(papel?: string | null): string {
    return this.papeis.find((p) => p.valor === papel)?.descricao ?? '';
  }

  private explicar(err: unknown, alternativa: string): string {
    const erro = err as { error?: { message?: string } | string };

    if (typeof erro?.error === 'string' && erro.error.trim()) return erro.error;
    if (typeof erro?.error === 'object' && erro.error?.message) return erro.error.message;

    return alternativa;
  }
}
