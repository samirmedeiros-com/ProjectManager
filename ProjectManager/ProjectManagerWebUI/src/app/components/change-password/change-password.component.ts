import { Component, OnInit, ChangeDetectionStrategy, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-change-password',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './change-password.component.html',
  styleUrl: './change-password.component.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ChangePasswordComponent implements OnInit {
  isOpen = false;
  loading = false;
  error = '';
  success = '';

  form = {
    currentPassword: '',
    newPassword: '',
    confirmPassword: ''
  };

  submitted = false;

  constructor(
    private authService: AuthService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit() {}

  openModal() {
    this.isOpen = true;
    this.resetForm();
  }

  closeModal() {
    this.isOpen = false;
    this.resetForm();
  }

  resetForm() {
    this.form = {
      currentPassword: '',
      newPassword: '',
      confirmPassword: ''
    };
    this.submitted = false;
    this.error = '';
    this.success = '';
  }

  validateForm(): boolean {
    this.error = '';

    if (!this.form.currentPassword) {
      this.error = 'A password atual é obrigatória';
      return false;
    }

    if (!this.form.newPassword) {
      this.error = 'A nova password é obrigatória';
      return false;
    }

    if (this.form.newPassword.length < 6) {
      this.error = 'A nova password deve ter pelo menos 6 caracteres';
      return false;
    }

    if (this.form.newPassword === this.form.currentPassword) {
      this.error = 'A nova password não pode ser igual à password atual';
      return false;
    }

    if (!this.form.confirmPassword) {
      this.error = 'Confirme a nova password';
      return false;
    }

    if (this.form.newPassword !== this.form.confirmPassword) {
      this.error = 'As passwords não coincidem';
      return false;
    }

    return true;
  }

  onSubmit() {
    this.submitted = true;
    this.error = '';
    this.success = '';

    if (!this.validateForm()) {
      return;
    }

    this.loading = true;

    this.authService.changePassword(
      this.form.currentPassword,
      this.form.newPassword
    ).subscribe({
      next: (response) => {
        this.loading = false;
        this.success = 'Password alterada com sucesso!';
        this.cdr.markForCheck();
        setTimeout(() => {
          this.closeModal();
          this.cdr.markForCheck();
        }, 2000);
      },
      error: (error) => {
        this.loading = false;
        this.error = error.error?.message || 'Erro ao alterar a password. Verifique se a password atual está correta.';
        this.cdr.markForCheck();
      }
    });
  }
}
