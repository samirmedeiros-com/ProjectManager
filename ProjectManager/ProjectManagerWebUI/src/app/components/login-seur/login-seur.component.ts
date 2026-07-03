import { Component, ChangeDetectorRef } from '@angular/core';
import { Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { SeurAuthService, SeurLoginRequest } from '../../services/seur-auth.service';

@Component({
  selector: 'app-login-seur',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './login-seur.component.html',
  styleUrls: ['./login-seur.component.css']
})
export class LoginSeurComponent {
  form: SeurLoginRequest = { email: '', password: '' };
  loading = false;
  submitted = false;
  error = '';

  showForgotModal = false;
  forgotEmail = '';
  forgotLoading = false;
  showResultModal = false;
  resultSuccess = false;
  resultMessage = '';

  constructor(
    private seurAuthService: SeurAuthService,
    private router: Router,
    private cdr: ChangeDetectorRef
  ) {}

  onSubmit() {
    this.submitted = true;
    this.error = '';
    if (!this.form.email || !this.form.password) return;

    this.loading = true;
    this.seurAuthService.login(this.form).subscribe({
      next: (response) => {
        if (response.success) {
          this.router.navigate(['/seur/dashboard']);
        } else {
          this.error = response.message || 'Falha no login';
        }
        this.loading = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.error = 'Falha no login. Por favor, verifique as suas credenciais.';
        this.loading = false;
        this.cdr.detectChanges();
      }
    });
  }

  openForgotModal() {
    this.forgotEmail = this.form.email || '';
    this.showForgotModal = true;
    this.cdr.detectChanges();
  }

  closeForgotModal() {
    this.showForgotModal = false;
    this.cdr.detectChanges();
  }

  submitForgot() {
    if (!this.forgotEmail.trim() || !this.forgotEmail.includes('@')) {
      this.showForgotModal = false;
      this.resultSuccess = false;
      this.resultMessage = 'Por favor, introduza um email válido';
      this.showResultModal = true;
      this.cdr.detectChanges();
      return;
    }

    this.forgotLoading = true;
    this.seurAuthService.forgotPassword(this.forgotEmail.trim()).subscribe({
      next: (res) => {
        this.forgotLoading = false;
        this.showForgotModal = false;
        this.resultSuccess = res.success;
        this.resultMessage = res.message;
        this.showResultModal = true;
        this.cdr.detectChanges();
      },
      error: () => {
        this.forgotLoading = false;
        this.showForgotModal = false;
        this.resultSuccess = false;
        this.resultMessage = 'Erro ao processar o pedido. Tente novamente.';
        this.showResultModal = true;
        this.cdr.detectChanges();
      }
    });
  }

  closeResultModal() {
    this.showResultModal = false;
    this.cdr.detectChanges();
  }

  goBack() {
    this.router.navigate(['/portal']);
  }
}
