import { Component, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { SeurUsersService } from '../../services/seur-users.service';

@Component({
  selector: 'app-seur-change-password',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './seur-change-password.component.html',
  styleUrls: ['./seur-change-password.component.css']
})
export class SeurChangePasswordComponent {
  isOpen = false;
  loading = false;
  erro = '';
  sucesso = '';

  form = { currentPassword: '', newPassword: '', confirmPassword: '' };

  constructor(
    private usersService: SeurUsersService,
    private cdr: ChangeDetectorRef
  ) {}

  open() {
    this.isOpen = true;
    this.form = { currentPassword: '', newPassword: '', confirmPassword: '' };
    this.erro = '';
    this.sucesso = '';
    this.cdr.detectChanges();
  }

  close() {
    this.isOpen = false;
    this.cdr.detectChanges();
  }

  submeter() {
    this.erro = '';
    this.sucesso = '';

    if (!this.form.currentPassword) { this.erro = 'A password atual é obrigatória'; return; }
    if (!this.form.newPassword || this.form.newPassword.length < 6) { this.erro = 'A nova password deve ter pelo menos 6 caracteres'; return; }
    if (this.form.newPassword === this.form.currentPassword) { this.erro = 'A nova password não pode ser igual à atual'; return; }
    if (this.form.newPassword !== this.form.confirmPassword) { this.erro = 'As passwords não coincidem'; return; }

    this.loading = true;
    this.usersService.changePassword(this.form.currentPassword, this.form.newPassword).subscribe({
      next: () => {
        this.loading = false;
        this.sucesso = 'Password alterada com sucesso!';
        this.cdr.detectChanges();
        setTimeout(() => { this.close(); }, 2000);
      },
      error: (err) => {
        this.loading = false;
        this.erro = err?.error?.message || 'Erro ao alterar a password. Verifique se a password atual está correta.';
        this.cdr.detectChanges();
      }
    });
  }
}
