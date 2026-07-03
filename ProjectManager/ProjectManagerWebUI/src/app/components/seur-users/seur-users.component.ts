import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { SeurUsersService } from '../../services/seur-users.service';
import { SeurUserDetail, CreateSeurUserDto } from '../../models/seur-user.model';

@Component({
  selector: 'app-seur-users',
  standalone: true,
  imports: [CommonModule, FormsModule, DatePipe],
  templateUrl: './seur-users.component.html',
  styleUrls: ['./seur-users.component.css']
})
export class SeurUsersComponent implements OnInit {
  users: SeurUserDetail[] = [];
  loading = false;
  erro = '';

  showCreateModal = false;
  creating = false;
  erroCreate = '';
  newUser: CreateSeurUserDto = { email: '', fullName: '', role: 'Utilizador' };

  showPasswordModal = false;
  modalPassword = '';
  modalEmail = '';
  modalEmailSent = false;
  modalTitle = '';

  deactivatingId: number | null = null;

  readonly roles = [
    { value: 'Utilizador', label: 'Utilizador' },
    { value: 'Admin', label: 'Administrador' }
  ];

  constructor(
    private usersService: SeurUsersService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit() {
    this.loadUsers();
  }

  loadUsers() {
    this.loading = true;
    this.erro = '';
    this.usersService.getUsers().subscribe({
      next: (data) => { this.users = data; this.loading = false; this.cdr.detectChanges(); },
      error: (err) => { this.erro = 'Erro ao carregar utilizadores: ' + (err?.status ?? ''); this.loading = false; this.cdr.detectChanges(); }
    });
  }

  abrirCriar() {
    this.newUser = { email: '', fullName: '', role: 'Utilizador' };
    this.erroCreate = '';
    this.showCreateModal = true;
    this.cdr.detectChanges();
  }

  fecharCriar() {
    this.showCreateModal = false;
    this.cdr.detectChanges();
  }

  criarUtilizador() {
    if (!this.newUser.email || !this.newUser.fullName) {
      this.erroCreate = 'Email e nome são obrigatórios';
      this.cdr.detectChanges();
      return;
    }
    this.creating = true;
    this.erroCreate = '';
    this.usersService.createUser(this.newUser).subscribe({
      next: (res) => {
        this.creating = false;
        this.showCreateModal = false;
        this.users = [...this.users, res.user];
        this.modalTitle = 'Utilizador Criado';
        this.modalEmail = res.user.email;
        this.modalPassword = res.tempPassword;
        this.modalEmailSent = res.emailSent;
        this.showPasswordModal = true;
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.creating = false;
        this.erroCreate = err?.error?.message || 'Erro ao criar utilizador';
        this.cdr.detectChanges();
      }
    });
  }

  resetarPassword(user: SeurUserDetail) {
    if (!confirm(`Resetar password de ${user.fullName}?`)) return;
    this.usersService.resetPassword(user.id).subscribe({
      next: (res) => {
        this.modalTitle = 'Password Reposta';
        this.modalEmail = user.email;
        this.modalPassword = res.tempPassword;
        this.modalEmailSent = res.emailSent;
        this.showPasswordModal = true;
        this.cdr.detectChanges();
      },
      error: () => { this.cdr.detectChanges(); }
    });
  }

  desativarUtilizador(user: SeurUserDetail) {
    if (!confirm(`Desativar o utilizador ${user.fullName}? Esta acção não pode ser desfeita facilmente.`)) return;
    this.deactivatingId = user.id;
    this.usersService.deactivateUser(user.id).subscribe({
      next: () => {
        this.users = this.users.map(u => u.id === user.id ? { ...u, isActive: false } : u);
        this.deactivatingId = null;
        this.cdr.detectChanges();
      },
      error: () => { this.deactivatingId = null; this.cdr.detectChanges(); }
    });
  }

  fecharPasswordModal() {
    this.showPasswordModal = false;
    this.cdr.detectChanges();
  }

  roleLabel(role?: string): string {
    return role === 'Admin' ? 'Administrador' : 'Utilizador';
  }
}
