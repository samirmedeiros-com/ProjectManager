import { Component, OnInit, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { SeurAuthService, SeurLoginRequest } from '../../services/seur-auth.service';

/** Identidade de um ecrã de login, vinda do `data` da rota. */
interface LoginTema {
  titulo: string;
  subtitulo: string;
  /** Cor de destaque (botão/realce). Cada app tem a sua para não parecerem o mesmo ecrã. */
  cor: string;
  /** Para onde ir depois de entrar, se a rota de origem não trouxer returnUrl. */
  returnUrlPadrao: string;
  /** Iniciais mostradas no emblema (ex.: "GD", "OS"). */
  sigla: string;
}

/**
 * Ecrã de login partilhado, personalizado por rota.
 *
 * A Gestão de Dados e a Consulta OpenSearch usam a **mesma base de utilizadores** da Gestão
 * SEUR — autenticam no mesmo endpoint e guardam o mesmo `seur_token`. O que muda é a
 * identidade: título, subtítulo e cor vêm do `data` da rota, para cada aplicação ter o seu
 * ecrã de entrada em vez de mandar toda a gente para o login do SEUR.
 *
 * O fluxo (login + recuperação de password) é o do `login-seur`; só a apresentação difere.
 */
@Component({
  selector: 'app-app-login',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './app-login.component.html',
  styleUrls: ['./app-login.component.scss'],
})
export class AppLoginComponent implements OnInit {
  tema: LoginTema = {
    titulo: 'Entrar',
    subtitulo: 'Aceda à sua conta',
    cor: '#534ab7',
    returnUrlPadrao: '/portal',
    sigla: '·',
  };

  form: SeurLoginRequest = { email: '', password: '' };
  loading = signal(false);
  submitted = signal(false);
  error = signal('');

  showForgotModal = signal(false);
  forgotEmail = '';
  forgotLoading = signal(false);
  showResultModal = signal(false);
  resultSuccess = signal(false);
  resultMessage = signal('');

  constructor(
    private seurAuth: SeurAuthService,
    private router: Router,
    private route: ActivatedRoute,
  ) {}

  ngOnInit(): void {
    const data = this.route.snapshot.data as Partial<LoginTema>;
    this.tema = { ...this.tema, ...data };

    // Se já há sessão SEUR válida, não vale a pena pedir login outra vez.
    if (this.seurAuth.isAuthenticated()) {
      this.router.navigate([this.returnUrl()]);
    }
  }

  private returnUrl(): string {
    return this.route.snapshot.queryParams['returnUrl'] || this.tema.returnUrlPadrao;
  }

  onSubmit(): void {
    this.submitted.set(true);
    this.error.set('');
    if (!this.form.email || !this.form.password) return;

    this.loading.set(true);
    this.seurAuth.login(this.form).subscribe({
      next: (resposta) => {
        this.loading.set(false);
        if (resposta.success) {
          this.router.navigate([this.returnUrl()]);
        } else {
          this.error.set(resposta.message || 'Falha no login.');
        }
      },
      error: () => {
        this.loading.set(false);
        this.error.set('Falha no login. Verifique as suas credenciais.');
      },
    });
  }

  abrirRecuperar(): void {
    this.forgotEmail = this.form.email || '';
    this.showForgotModal.set(true);
  }

  fecharRecuperar(): void {
    this.showForgotModal.set(false);
  }

  submeterRecuperar(): void {
    if (!this.forgotEmail.trim() || !this.forgotEmail.includes('@')) {
      this.showForgotModal.set(false);
      this.resultSuccess.set(false);
      this.resultMessage.set('Introduza um email válido.');
      this.showResultModal.set(true);
      return;
    }

    this.forgotLoading.set(true);
    this.seurAuth.forgotPassword(this.forgotEmail.trim()).subscribe({
      next: (res) => {
        this.forgotLoading.set(false);
        this.showForgotModal.set(false);
        this.resultSuccess.set(res.success);
        this.resultMessage.set(res.message);
        this.showResultModal.set(true);
      },
      error: () => {
        this.forgotLoading.set(false);
        this.showForgotModal.set(false);
        this.resultSuccess.set(false);
        this.resultMessage.set('Erro ao processar o pedido. Tente novamente.');
        this.showResultModal.set(true);
      },
    });
  }

  fecharResultado(): void {
    this.showResultModal.set(false);
  }

  voltarAoPortal(): void {
    this.router.navigate(['/portal']);
  }
}
