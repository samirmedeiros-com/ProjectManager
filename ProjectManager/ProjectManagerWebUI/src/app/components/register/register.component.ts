import { Component, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './register.component.html',
  styleUrls: ['./register.component.css'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class RegisterComponent {
  form = {
    fullName: '',
    email: '',
    role: 'Utilizador'
  };

  loading = false;
  errorMessage = '';
  successMessage = '';

  roles = [
    { value: 'Owner', label: 'Owner' },
    { value: 'Admin', label: 'Admin' },
    { value: 'Gestor', label: 'Gestor de Projetos' },
    { value: 'Utilizador', label: 'Utilizador' }
  ];

  constructor(private authService: AuthService, private router: Router) {}

  register() {
    this.errorMessage = '';
    this.successMessage = '';

    // Validações
    if (!this.form.fullName.trim()) {
      this.errorMessage = 'Nome completo é obrigatório';
      return;
    }

    if (!this.form.email.trim()) {
      this.errorMessage = 'Email é obrigatório';
      return;
    }

    if (!this.form.email.includes('@')) {
      this.errorMessage = 'Email inválido';
      return;
    }

    this.loading = true;

    this.authService.register({
      fullName: this.form.fullName.trim(),
      email: this.form.email.trim(),
      role: this.form.role
    }).subscribe({
      next: (response) => {
        this.loading = false;
        this.successMessage = 'Registo realizado com sucesso! Redirecionando...';
        setTimeout(() => {
          this.router.navigate(['/login']);
        }, 2000);
      },
      error: (error) => {
        this.loading = false;
        this.errorMessage = error.error?.message || 'Erro ao registrar. Tente novamente.';
      }
    });
  }

  goToLogin() {
    this.router.navigate(['/login']);
  }
}
