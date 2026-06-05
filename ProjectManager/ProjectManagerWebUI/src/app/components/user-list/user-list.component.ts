import { Component, ChangeDetectionStrategy, ChangeDetectorRef, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { User, UpdateUserRequest } from '../../models/user.model';
import { Setor } from '../../models/setor.model';
import { UserService } from '../../services/user.service';
import { SetorService } from '../../services/setor.service';
import { AuthService } from '../../services/auth.service';

interface CreateUserForm {
  fullName: string;
  email: string;
  password: string;
  role: string;
  setorIds: number[];
}

@Component({
  selector: 'app-user-list',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './user-list.component.html',
  styleUrls: ['./user-list.component.css'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class UserListComponent implements OnInit {
  users: User[] = [];
  loading = false;
  error = '';
  success = '';

  showForm = false;
  editingUserId: number | null = null;

  setores: Setor[] = [];
  setoresLoading = false;
  currentUserSetores: number[] = [];

  userRoles = [
    { value: 'Owner', label: 'Owner' },
    { value: 'Admin', label: 'Admin' },
    { value: 'Gestor', label: 'Gestor de Projetos' },
    { value: 'Utilizador', label: 'Utilizador' }
  ];

  formData: CreateUserForm = {
    fullName: '',
    email: '',
    password: '',
    role: 'Utilizador',
    setorIds: []
  };

  showUserMenu = false;

  get currentUser() {
    return this.authService.currentUserValue;
  }

  constructor(
    private userService: UserService,
    private setorService: SetorService,
    private authService: AuthService,
    private router: Router,
    private cdr: ChangeDetectorRef
  ) { }

  ngOnInit() {
    this.loadSetores();
    const userRole = this.currentUser?.role;
    if (userRole === 'Gestor') {
      this.loadCurrentUserSetores();
    }
    this.loadUsers();
  }

  isAdmin(): boolean {
    return this.currentUser?.role === 'Admin';
  }

  loadSetores() {
    this.setoresLoading = true;
    this.setorService.getAllSetores().subscribe({
      next: (data) => {
        this.setores = data;
        this.setoresLoading = false;
        this.cdr.markForCheck();
      },
      error: (err) => {
        console.error('Erro ao carregar setores', err);
        this.setoresLoading = false;
        this.cdr.markForCheck();
      }
    });
  }

  loadCurrentUserSetores() {
    if (this.currentUser?.role !== 'Gestor') return;

    this.userService.getCurrentUserSetores().subscribe({
      next: (setores) => {
        this.currentUserSetores = setores.map(s => s.id);
        this.cdr.markForCheck();
      },
      error: (err) => {
        console.error('Erro ao carregar setores do utilizador', err);
      }
    });
  }

  getAvailableSetores(): Setor[] {
    const userRole = this.currentUser?.role;
    if (userRole === 'Admin') {
      return this.setores;
    }
    if (userRole === 'Gestor') {
      return this.setores.filter(s => this.currentUserSetores.includes(s.id));
    }
    return [];
  }

  getAvailableRoles(): any[] {
    const userRole = this.currentUser?.role;
    if (userRole === 'Admin') {
      return this.userRoles;
    }
    if (userRole === 'Gestor') {
      return this.userRoles.filter(r => r.value === 'Gestor' || r.value === 'Utilizador');
    }
    return [];
  }

  canEditUser(user: User): boolean {
    const userRole = this.currentUser?.role;
    if (userRole === 'Admin') return true;
    if (userRole === 'Gestor') {
      const userSetores = user.setores?.map(s => s.id) || [];
      return userSetores.some(setorId => this.currentUserSetores.includes(setorId));
    }
    return false;
  }

  loadUsers() {
    this.loading = true;
    this.error = '';
    this.userService.getAllUsers().subscribe({
      next: (data) => {
        this.users = data;
        this.loading = false;
        this.cdr.markForCheck();
      },
      error: (err) => {
        this.error = 'Erro ao carregar utilizadores';
        console.error(err);
        this.loading = false;
        this.cdr.markForCheck();
      }
    });
  }

  openForm() {
    this.editingUserId = null;
    this.formData = { fullName: '', email: '', password: '', role: 'Utilizador', setorIds: [] };
    this.showForm = true;
    this.error = '';
    this.success = '';
  }

  editUser(user: User) {
    this.editingUserId = user.id;
    this.formData = {
      fullName: user.fullName,
      email: user.email,
      password: '',
      role: user.role || 'Utilizador',
      setorIds: user.setores?.map(s => s.id) || []
    };
    this.showForm = true;
    this.error = '';
    this.success = '';
  }

  saveUser() {
    if (!this.formData.fullName.trim()) {
      this.error = 'O nome é obrigatório';
      this.cdr.markForCheck();
      return;
    }

    if (!this.formData.email.trim()) {
      this.error = 'O email é obrigatório';
      this.cdr.markForCheck();
      return;
    }

    if (!this.editingUserId && !this.formData.password) {
      this.error = 'A password é obrigatória para novos utilizadores';
      this.cdr.markForCheck();
      return;
    }

    const userRole = this.currentUser?.role;
    if (userRole === 'Gestor') {
      const allowedRoles = ['Gestor', 'Utilizador'];
      if (!allowedRoles.includes(this.formData.role)) {
        this.error = `Gestores apenas podem criar ${allowedRoles.join(' e ')}`;
        this.cdr.markForCheck();
        return;
      }

      const availableSetores = this.getAvailableSetores().map(s => s.id);
      const invalidSetores = this.formData.setorIds.filter(id => !availableSetores.includes(id));
      if (invalidSetores.length > 0) {
        this.error = 'Tem setores selecionados sem permissão de acesso';
        this.cdr.markForCheck();
        return;
      }
    }

    if (this.editingUserId) {
      this.updateUser();
    } else {
      this.createUser();
    }
  }

  createUser() {
    this.userService.registerUser({
      fullName: this.formData.fullName.trim(),
      email: this.formData.email.trim(),
      password: this.formData.password,
      role: this.formData.role
    }).subscribe({
      next: (response) => {
        if (response.user) {
          this.users.push(response.user);
        }
        this.success = 'Utilizador criado com sucesso!';
        this.closeForm();
        this.cdr.markForCheck();
      },
      error: (err) => {
        this.error = err.error?.message || 'Erro ao criar utilizador';
        console.error(err);
        this.cdr.markForCheck();
      }
    });
  }

  updateUser() {
    if (!this.editingUserId) return;

    const user = this.users.find(u => u.id === this.editingUserId);
    if (!user) return;

    const updateRequest: UpdateUserRequest = {
      fullName: this.formData.fullName.trim(),
      email: this.formData.email.trim(),
      role: this.formData.role,
      isActive: user.isActive,
      setorIds: this.formData.setorIds
    };

    this.userService.updateUser(this.editingUserId, updateRequest).subscribe({
      next: (updatedUser) => {
        const index = this.users.findIndex(u => u.id === this.editingUserId);
        if (index !== -1) {
          this.users[index] = updatedUser;
        }
        this.success = 'Utilizador atualizado com sucesso!';
        this.closeForm();
        this.cdr.markForCheck();
      },
      error: (err) => {
        this.error = 'Erro ao atualizar utilizador';
        console.error(err);
        this.cdr.markForCheck();
      }
    });
  }

  toggleUserActive(user: User) {
    if (user.isActive) {
      this.deactivateUser(user.id);
    } else {
      this.activateUser(user.id);
    }
  }

  deactivateUser(id: number) {
    this.userService.deactivateUser(id).subscribe({
      next: (updatedUser) => {
        const index = this.users.findIndex(u => u.id === id);
        if (index !== -1) {
          this.users[index] = updatedUser;
        }
        this.success = 'Utilizador inativado com sucesso!';
        this.cdr.markForCheck();
      },
      error: (err) => {
        this.error = 'Erro ao inativar utilizador';
        console.error(err);
        this.cdr.markForCheck();
      }
    });
  }

  activateUser(id: number) {
    this.userService.activateUser(id).subscribe({
      next: (updatedUser) => {
        const index = this.users.findIndex(u => u.id === id);
        if (index !== -1) {
          this.users[index] = updatedUser;
        }
        this.success = 'Utilizador ativado com sucesso!';
        this.cdr.markForCheck();
      },
      error: (err) => {
        this.error = 'Erro ao ativar utilizador';
        console.error(err);
        this.cdr.markForCheck();
      }
    });
  }

  deleteUser(id: number) {
    if (!confirm('Tem certeza que deseja deletar este utilizador?')) return;

    this.userService.deleteUser(id).subscribe({
      next: () => {
        this.users = this.users.filter(u => u.id !== id);
        this.success = 'Utilizador deletado com sucesso!';
        this.cdr.markForCheck();
      },
      error: (err) => {
        this.error = 'Erro ao deletar utilizador';
        console.error(err);
        this.cdr.markForCheck();
      }
    });
  }

  closeForm() {
    this.showForm = false;
    this.editingUserId = null;
    this.formData = { fullName: '', email: '', password: '', role: 'Utilizador', setorIds: [] };
  }

  goToDashboard() {
    this.router.navigate(['/dashboard']);
  }

  toggleUserMenu() {
    this.showUserMenu = !this.showUserMenu;
    this.cdr.markForCheck();
  }

  openChangePassword() {
    this.showUserMenu = false;
    this.cdr.markForCheck();
  }

  logout() {
    this.authService.logout();
    this.router.navigate(['/login']);
  }

  getRoleLabel(role: string): string {
    const roleObj = this.userRoles.find(r => r.value === role);
    return roleObj ? roleObj.label : role;
  }
}
