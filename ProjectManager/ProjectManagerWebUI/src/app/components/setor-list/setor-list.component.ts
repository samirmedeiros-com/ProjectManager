import { Component, ChangeDetectionStrategy, ChangeDetectorRef, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { SetorService } from '../../services/setor.service';
import { Setor } from '../../models/setor.model';
import { AuthService } from '../../services/auth.service';

interface CreateSetorRequest {
  name: string;
  description?: string;
}

interface UpdateSetorRequest {
  name: string;
  description?: string;
  isActive: boolean;
}

@Component({
  selector: 'app-setor-list',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './setor-list.component.html',
  styleUrls: ['./setor-list.component.css'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class SetorListComponent implements OnInit {
  setores: Setor[] = [];
  loading = false;
  error = '';
  success = '';

  showForm = false;
  showUserMenu = false;
  editingSetorId: number | null = null;

  formData = {
    name: '',
    description: ''
  };

  constructor(
    private setorService: SetorService,
    private authService: AuthService,
    private router: Router,
    private cdr: ChangeDetectorRef
  ) { }

  get currentUser() {
    return this.authService.currentUserValue;
  }

  toggleUserMenu() {
    this.showUserMenu = !this.showUserMenu;
    this.cdr.markForCheck();
  }

  openChangePassword() {
    this.showUserMenu = false;
    // Implementar modal de alterar password se necessário
    this.cdr.markForCheck();
  }

  logout() {
    this.authService.logout();
    this.router.navigate(['/login']);
  }

  ngOnInit() {
    if (!this.isAdmin()) {
      this.router.navigate(['/dashboard']);
      return;
    }
    this.loadSetores();
  }

  isAdmin(): boolean {
    return this.currentUser?.role === 'Admin';
  }

  loadSetores() {
    this.loading = true;
    this.error = '';
    this.setorService.getAllSetores().subscribe({
      next: (data) => {
        this.setores = data;
        this.loading = false;
        this.cdr.markForCheck();
      },
      error: (err) => {
        this.error = 'Erro ao carregar setores';
        console.error(err);
        this.loading = false;
        this.cdr.markForCheck();
      }
    });
  }

  openForm() {
    this.editingSetorId = null;
    this.formData = { name: '', description: '' };
    this.showForm = true;
    this.error = '';
    this.success = '';
  }

  editSetor(setor: Setor) {
    this.editingSetorId = setor.id;
    this.formData = { name: setor.name, description: setor.description || '' };
    this.showForm = true;
    this.error = '';
    this.success = '';
  }

  saveSetor() {
    if (!this.formData.name.trim()) {
      this.error = 'O nome do setor é obrigatório';
      this.cdr.markForCheck();
      return;
    }

    if (this.editingSetorId) {
      this.updateSetor();
    } else {
      this.createSetor();
    }
  }

  createSetor() {
    const request: CreateSetorRequest = {
      name: this.formData.name.trim(),
      description: this.formData.description.trim() || undefined
    };

    this.setorService.createSetor(request).subscribe({
      next: (newSetor) => {
        this.setores.push(newSetor);
        this.success = 'Setor criado com sucesso!';
        this.closeForm();
        this.loadSetores();
        this.cdr.markForCheck();
      },
      error: (err) => {
        this.error = 'Erro ao criar setor';
        console.error(err);
        this.cdr.markForCheck();
      }
    });
  }

  updateSetor() {
    if (!this.editingSetorId) return;

    const setor = this.setores.find(s => s.id === this.editingSetorId);
    if (!setor) return;

    const request: UpdateSetorRequest = {
      name: this.formData.name.trim(),
      description: this.formData.description.trim() || undefined,
      isActive: setor.isActive
    };

    this.setorService.updateSetor(this.editingSetorId, request).subscribe({
      next: (updatedSetor) => {
        const index = this.setores.findIndex(s => s.id === this.editingSetorId);
        if (index !== -1) {
          this.setores[index] = updatedSetor;
        }
        this.success = 'Setor atualizado com sucesso!';
        this.closeForm();
        this.cdr.markForCheck();
      },
      error: (err) => {
        this.error = 'Erro ao atualizar setor';
        console.error(err);
        this.cdr.markForCheck();
      }
    });
  }

  deleteSetor(id: number) {
    if (!confirm('Tem certeza que deseja deletar este setor?')) return;

    this.setorService.deleteSetor(id).subscribe({
      next: () => {
        this.setores = this.setores.filter(s => s.id !== id);
        this.success = 'Setor deletado com sucesso!';
        this.cdr.markForCheck();
      },
      error: (err) => {
        if (err.status === 400) {
          this.error = 'Não é possível deletar um setor que contém utilizadores.';
        } else {
          this.error = 'Erro ao deletar setor';
        }
        console.error(err);
        this.cdr.markForCheck();
      }
    });
  }

  closeForm() {
    this.showForm = false;
    this.editingSetorId = null;
    this.formData = { name: '', description: '' };
  }

  goToDashboard() {
    this.router.navigate(['/dashboard']);
  }
}
