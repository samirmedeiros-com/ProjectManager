import { Component, ChangeDetectorRef } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '../../services/auth.service';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { LoginRequest } from '../../models/user.model';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.css']
})
export class LoginComponent {
  form: LoginRequest = { email: '', password: '' };
  loading = false;
  submitted = false;
  error = '';

  // Forgot Password Modal - Email Input
  showForgotPasswordModal = false;
  forgotPasswordEmail = '';
  forgotPasswordLoading = false;

  // Forgot Password Modal - Message
  showForgotPasswordMessageModal = false;
  forgotPasswordMessageType: 'success' | 'error' = 'success'; // success ou error
  forgotPasswordMessage = '';

  constructor(
    private authService: AuthService,
    private router: Router,
    private changeDetectorRef: ChangeDetectorRef
  ) { }

  onSubmit() {
    this.submitted = true;
    this.error = '';

    if (!this.form.email || !this.form.password) {
      return;
    }

    this.loading = true;
    this.authService.login(this.form).subscribe(
      (response) => {
        if (response.success) {
          this.router.navigate(['/dashboard']);
        } else {
          this.error = response.message || 'Falha no login';
        }
        this.loading = false;
      },
      (error) => {
        this.error = 'Falha no login. Por favor, verifique suas credenciais.';
        this.loading = false;
      }
    );
  }

  openForgotPasswordModal() {
    this.showForgotPasswordModal = true;
    this.forgotPasswordEmail = '';
  }

  closeForgotPasswordModal() {
    this.showForgotPasswordModal = false;
    this.forgotPasswordEmail = '';
  }

  closeForgotPasswordMessageModal() {
    console.log('closeForgotPasswordMessageModal called');
    this.showForgotPasswordMessageModal = false;
    this.forgotPasswordMessage = '';
  }

  submitForgotPassword() {
    console.log('submitForgotPassword called with email:', this.forgotPasswordEmail);

    if (!this.forgotPasswordEmail.trim()) {
      console.log('Email vazio - mostrando erro');
      this.forgotPasswordMessageType = 'error';
      this.forgotPasswordMessage = 'Por favor, introduza o seu email';
      this.closeForgotPasswordModal();
      this.showForgotPasswordMessageModal = true;
      this.changeDetectorRef.detectChanges();
      console.log('showForgotPasswordMessageModal:', this.showForgotPasswordMessageModal);
      return;
    }

    if (!this.forgotPasswordEmail.includes('@')) {
      this.forgotPasswordMessageType = 'error';
      this.forgotPasswordMessage = 'Email inválido';
      this.closeForgotPasswordModal();
      this.showForgotPasswordMessageModal = true;
      this.changeDetectorRef.detectChanges();
      return;
    }

    this.forgotPasswordLoading = true;
    this.authService.forgotPassword(this.forgotPasswordEmail).subscribe({
      next: (response) => {
        console.log('Response recebido:', response);
        this.forgotPasswordLoading = false;
        this.closeForgotPasswordModal();

        if (response.success) {
          this.forgotPasswordMessageType = 'success';
          this.forgotPasswordMessage = response.message || 'Password enviada para o seu email!';
        } else {
          this.forgotPasswordMessageType = 'error';
          this.forgotPasswordMessage = response.message || 'Erro ao recuperar password';
        }

        console.log('Abrindo modal de mensagem:');
        console.log('showForgotPasswordMessageModal:', this.showForgotPasswordMessageModal);
        console.log('forgotPasswordMessage:', this.forgotPasswordMessage);
        console.log('forgotPasswordMessageType:', this.forgotPasswordMessageType);
        this.showForgotPasswordMessageModal = true;
        this.changeDetectorRef.detectChanges();
        console.log('showForgotPasswordMessageModal após:', this.showForgotPasswordMessageModal);
      },
      error: (error) => {
        console.log('Erro recebido:', error);
        this.forgotPasswordLoading = false;
        this.closeForgotPasswordModal();

        this.forgotPasswordMessageType = 'error';
        if (error.error && typeof error.error === 'object' && error.error.message) {
          this.forgotPasswordMessage = error.error.message;
        } else if (error.error && typeof error.error === 'string') {
          this.forgotPasswordMessage = error.error;
        } else {
          this.forgotPasswordMessage = error.message || 'Erro ao recuperar password. Tente novamente.';
        }

        console.log('Abrindo modal de erro - mensagem:', this.forgotPasswordMessage);
        this.showForgotPasswordMessageModal = true;
        this.changeDetectorRef.detectChanges();
        console.log('showForgotPasswordMessageModal após erro:', this.showForgotPasswordMessageModal);
      }
    });
  }
}
