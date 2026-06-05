import { Component, OnInit, OnDestroy, ChangeDetectionStrategy, ChangeDetectorRef } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AuthService } from '../../services/auth.service';
import { ProjectService } from '../../services/project.service';
import { TaskService } from '../../services/task.service';
import { UserService } from '../../services/user.service';
import { Project } from '../../models/project.model';
import { ProjectTask } from '../../models/task.model';
import { ProjectMember } from '../../models/project-member.model';

@Component({
  selector: 'app-project-detail',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './project-detail.component.html',
  styleUrls: ['./project-detail.component.css'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ProjectDetailComponent implements OnInit, OnDestroy {
  project: Project | null = null;
  tasks: ProjectTask[] = [];
  members: ProjectMember[] = [];
  users: any[] = [];
  allUsers: any[] = [];
  gestorSetores: any[] = [];
  currentUser: any = null;
  loading = false;
  projectId: number = 0;
  activeTab: 'comments' | 'history' | 'evolution' = 'comments';
  newComment = '';
  comments: any[] = [];
  history: any[] = [];
  statusEvolution: any[] = [];
  showDeleteModal = false;
  isEditing = false;
  editForm: any = {};
  isSaving = false;
  private durationUpdateInterval: any;

  // User Menu
  showUserMenu = false;

  // Change Password Modal
  showChangePasswordModal = false;
  changePasswordLoading = false;
  changePasswordError = '';
  changePasswordSuccess = '';
  changePasswordForm = {
    currentPassword: '',
    newPassword: '',
    confirmPassword: ''
  };

  statusOptions = [
    { value: 'Planning', label: 'Planeamento' },
    { value: 'Released', label: 'Por Iniciar' },
    { value: 'Development', label: 'Desenvolvimento' },
    { value: 'Completed', label: 'Concluído' },
    { value: 'On Hold', label: 'Em Espera' },
    { value: 'Finished', label: 'Finalizado' }
  ];

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private projectService: ProjectService,
    private taskService: TaskService,
    private authService: AuthService,
    private userService: UserService,
    private cdr: ChangeDetectorRef
  ) { }

  ngOnInit() {
    this.projectId = parseInt(this.route.snapshot.paramMap.get('id') || '0');
    this.currentUser = this.authService.currentUserValue;
    this.loadProject();
    this.loadUsers();
    this.loadGestorSetores();
  }

  ngOnDestroy() {
    if (this.durationUpdateInterval) {
      clearInterval(this.durationUpdateInterval);
    }
  }

  private loadUsers() {
    // Carrega usuarios do setor do gestor (para Responsável)
    this.userService.getAllUsers().subscribe({
      next: (users) => {
        this.users = users;
        this.cdr.markForCheck();
      },
      error: (error) => {
        console.error('Erro ao carregar utilizadores:', error);
      }
    });

    // Carrega todos os usuarios (para Owner)
    this.userService.getAllUsersUnfiltered().subscribe({
      next: (allUsers) => {
        this.allUsers = allUsers;
        this.cdr.markForCheck();
      },
      error: (error) => {
        console.error('Erro ao carregar todos os utilizadores:', error);
      }
    });
  }

  private loadGestorSetores() {
    // Carrega setores do gestor (para editar setor do projeto)
    this.projectService.getUserSectors().subscribe({
      next: (setores) => {
        this.gestorSetores = setores;
        this.cdr.markForCheck();
      },
      error: (error) => {
        console.error('Erro ao carregar setores do gestor:', error);
      }
    });
  }

  loadProject() {
    this.loading = true;
    this.cdr.markForCheck();

    this.projectService.getById(this.projectId).subscribe(
      (data) => {
        this.project = data;
        this.members = data.members || [];
        this.tasks = data.tasks || [];
        this.loadComments();
        this.loadHistory();
        this.loading = false;
        this.cdr.markForCheck();
      },
      (error) => {
        console.error('Erro ao carregar projeto:', error.message);
        this.loading = false;
        this.cdr.markForCheck();
      }
    );
  }

  private loadComments() {
    this.projectService.getComments(this.projectId).subscribe({
      next: (comments) => {
        this.comments = comments.sort((a, b) => {
          const dateA = new Date(a.createdAt).getTime();
          const dateB = new Date(b.createdAt).getTime();
          return dateB - dateA;
        });
        this.cdr.markForCheck();
      },
      error: (error) => {
        console.error('Erro ao carregar comentários:', error);
      }
    });
  }

  goBack() {
    this.router.navigate(['/dashboard']);
  }

  logout() {
    this.authService.logout();
    this.router.navigate(['/login']);
  }

  isGestor(): boolean {
    const role = this.authService.currentUserValue?.role;
    return role === 'Admin' || role === 'Gestor';
  }

  isOwner(): boolean {
    return this.authService.currentUserValue?.role === 'Owner';
  }

  getStatusLabel(statusValue: string | undefined): string {
    if (!statusValue) return '';
    const status = this.statusOptions.find(s => s.value === statusValue);
    return status ? status.label : statusValue;
  }

  openFreshDeskTicket(freshDeskId: string | undefined) {
    if (freshDeskId) {
      const url = `https://suporte.dpd.pt/a/tickets/${freshDeskId}`;
      window.open(url, '_blank');
    }
  }

  addComment() {
    if (!this.newComment.trim()) return;

    this.projectService.addComment(this.projectId, this.newComment).subscribe({
      next: (comment) => {
        this.comments.unshift(comment);
        this.newComment = '';
        this.loadHistory();
        this.cdr.markForCheck();
      },
      error: (error) => {
        console.error('Erro ao adicionar comentário:', error);
      }
    });
  }

  private loadHistory() {
    this.projectService.getHistory(this.projectId).subscribe({
      next: (history) => {
        this.history = history;
        this.buildStatusEvolution();
        this.cdr.markForCheck();
      },
      error: (error) => {
        console.error('Erro ao carregar histórico:', error);
      }
    });
  }

  private buildStatusEvolution() {
    console.log('Building status evolution with history:', this.history);

    // Filtrar registros de status do histórico (vêm do banco de dados)
    const statusRecords = this.history
      .filter(h => h.type === 'status')
      .sort((a, b) => {
        const dateA = new Date(a.changedAt || a.date).getTime();
        const dateB = new Date(b.changedAt || b.date).getTime();
        return dateA - dateB;
      });

    console.log('Status records from history:', statusRecords);

    // Mapear os registros de status para evolução
    this.statusEvolution = statusRecords.map((record, index) => {
      const currentDate = new Date(record.changedAt || record.date);
      const nextRecord = statusRecords[index + 1];
      const isLastStatus = index === statusRecords.length - 1;
      const endDate = nextRecord ? new Date(nextRecord.changedAt || nextRecord.date) : new Date();

      const duration = this.calculateDuration(currentDate, endDate);

      return {
        fromStatus: record.fromStatus || 'Início',
        toStatus: record.toStatus,
        changedAt: currentDate,
        changedBy: record.changedBy || record.author,
        duration: duration,
        isLastStatus: isLastStatus,
        startTime: currentDate
      };
    });

    console.log('Status evolution:', this.statusEvolution);

    // Limpar intervalo anterior se existir
    if (this.durationUpdateInterval) {
      clearInterval(this.durationUpdateInterval);
    }

    // Atualizar duração do status atual a cada segundo
    this.durationUpdateInterval = setInterval(() => {
      const lastStatus = this.statusEvolution[this.statusEvolution.length - 1];
      if (lastStatus && lastStatus.isLastStatus) {
        const currentDate = new Date(lastStatus.startTime);
        const now = new Date();
        lastStatus.duration = this.calculateDuration(currentDate, now);
        this.cdr.markForCheck();
      }
    }, 1000); // Atualizar a cada 1 segundo
  }

  private calculateDuration(startDate: Date, endDate: Date): any {
    const diffMs = endDate.getTime() - startDate.getTime();
    const diffSecs = Math.floor(diffMs / 1000);

    const days = Math.floor(diffSecs / (24 * 3600));
    const hours = Math.floor((diffSecs % (24 * 3600)) / 3600);
    const minutes = Math.floor((diffSecs % 3600) / 60);
    const seconds = diffSecs % 60;

    return { days, hours, minutes, seconds };
  }

  getHistoryIcon(type: string): string {
    const icons: { [key: string]: string } = {
      created: '✨',
      status: '🔄',
      edited: '✏️',
      comment: '💬',
      manager: '👤',
      owner: '👑'
    };
    return icons[type] || '📌';
  }

  confirmDelete() {
    this.showDeleteModal = false;
    this.projectService.delete(this.projectId).subscribe(
      () => {
        this.router.navigate(['/dashboard']);
      },
      (error) => {
        console.error('Erro ao deletar projeto:', error);
      }
    );
  }

  startEdit() {
    this.editForm = {
      name: this.project?.name,
      description: this.project?.description,
      startDate: this.formatDateForInput(this.project?.startDate),
      endDate: this.formatDateForInput(this.project?.endDate),
      priority: this.project?.priority,
      status: this.project?.status,
      manager: this.project?.manager,
      ownerId: this.project?.ownerId,
      setorId: this.project?.setorId,
      freshDeskId: this.project?.freshDeskId
    };
    this.isEditing = true;
    this.cdr.markForCheck();
  }

  cancelEdit() {
    this.isEditing = false;
    this.editForm = {};
    this.cdr.markForCheck();
  }

  saveEdit() {
    if (!this.project) return;

    this.isSaving = true;
    this.cdr.markForCheck();

    const updateRequest = {
      name: this.editForm.name,
      description: this.editForm.description,
      startDate: this.editForm.startDate ? new Date(this.editForm.startDate) : undefined,
      endDate: this.editForm.endDate ? new Date(this.editForm.endDate) : undefined,
      status: this.editForm.status,
      priority: this.editForm.priority,
      ownerId: this.editForm.ownerId,
      setorId: this.editForm.setorId,
      freshDeskId: this.editForm.freshDeskId
    };

    this.projectService.update(this.projectId, updateRequest).subscribe(
      (updatedProject) => {
        // Se status mudou, registar histórico
        if (this.editForm.status !== this.project?.status) {
          this.projectService.updateStatus(this.projectId, this.editForm.status).subscribe();
        }

        // Se manager mudou, registar histórico
        if (this.editForm.manager !== this.project?.manager) {
          this.projectService.updateManager(this.projectId, this.editForm.manager).subscribe();
        }

        // Se owner mudou, registar no histórico
        if (this.editForm.ownerId !== this.project?.ownerId) {
          this.projectService.updateOwner(this.projectId, this.editForm.ownerId).subscribe();
        }

        this.project = updatedProject;
        this.isEditing = false;
        this.isSaving = false;
        this.loadHistory();
        this.cdr.markForCheck();
      },
      (error) => {
        console.error('Erro ao atualizar projeto:', error);
        this.isSaving = false;
        this.cdr.markForCheck();
      }
    );
  }

  private formatDateForInput(date: any): string {
    if (!date) return '';
    const d = new Date(date);
    const month = String(d.getMonth() + 1).padStart(2, '0');
    const day = String(d.getDate()).padStart(2, '0');
    const year = d.getFullYear();
    return `${year}-${month}-${day}`;
  }

  getUserNameByEmail(email: string): string {
    const user = this.users.find(u => u.email === email);
    return user ? user.fullName : email;
  }

  canEditProject(): boolean {
    if (this.isGestor()) return true;
    // Utilizador não pode editar se projeto está Concluído
    return this.project?.status !== 'Completed';
  }

  canAddComment(): boolean {
    if (this.isGestor()) return true;
    // Utilizador não pode comentar se projeto está Concluído
    return this.project?.status !== 'Completed';
  }

  getAvailableStatusOptions(): any[] {
    if (this.isGestor()) return this.statusOptions;

    // Utilizadores podem fazer transições específicas:
    // Released (Por Iniciar) → Development (Desenvolvimento)
    // Development (Desenvolvimento) → Completed (Concluído)
    const currentStatus = this.project?.status;

    if (currentStatus === 'Released') {
      return this.statusOptions.filter(s => s.value === 'Development');
    } else if (currentStatus === 'Development') {
      return this.statusOptions.filter(s => s.value === 'Completed');
    }

    // Nenhuma transição permitida para outros status
    return [];
  }

  isProjectFinalized(): boolean {
    return this.project?.status === 'Completed' && !this.isGestor();
  }

  toggleUserMenu() {
    this.showUserMenu = !this.showUserMenu;
    this.cdr.markForCheck();
  }

  openChangePassword() {
    this.showUserMenu = false;
    this.showChangePasswordModal = true;
    this.resetChangePasswordForm();
    this.cdr.markForCheck();
  }

  closeChangePasswordModal() {
    this.showChangePasswordModal = false;
    this.resetChangePasswordForm();
    this.cdr.markForCheck();
  }

  resetChangePasswordForm() {
    this.changePasswordForm = {
      currentPassword: '',
      newPassword: '',
      confirmPassword: ''
    };
    this.changePasswordError = '';
    this.changePasswordSuccess = '';
  }

  submitChangePassword() {
    this.changePasswordError = '';

    if (!this.changePasswordForm.currentPassword) {
      this.changePasswordError = 'A password atual é obrigatória';
      return;
    }

    if (!this.changePasswordForm.newPassword) {
      this.changePasswordError = 'A nova password é obrigatória';
      return;
    }

    if (this.changePasswordForm.newPassword.length < 6) {
      this.changePasswordError = 'A nova password deve ter pelo menos 6 caracteres';
      return;
    }

    if (this.changePasswordForm.newPassword === this.changePasswordForm.currentPassword) {
      this.changePasswordError = 'A nova password não pode ser igual à password atual';
      return;
    }

    if (!this.changePasswordForm.confirmPassword) {
      this.changePasswordError = 'Confirme a nova password';
      return;
    }

    if (this.changePasswordForm.newPassword !== this.changePasswordForm.confirmPassword) {
      this.changePasswordError = 'As passwords não coincidem';
      return;
    }

    this.changePasswordLoading = true;
    this.cdr.markForCheck();

    this.authService.changePassword(
      this.changePasswordForm.currentPassword,
      this.changePasswordForm.newPassword
    ).subscribe({
      next: (response) => {
        this.changePasswordLoading = false;
        this.changePasswordSuccess = 'Password alterada com sucesso!';
        this.cdr.markForCheck();
        setTimeout(() => {
          this.closeChangePasswordModal();
        }, 2000);
      },
      error: (error) => {
        this.changePasswordLoading = false;
        this.changePasswordError = error.error?.message || 'Erro ao alterar a password. Verifique se a password atual está correta.';
        this.cdr.markForCheck();
      }
    });
  }
}
