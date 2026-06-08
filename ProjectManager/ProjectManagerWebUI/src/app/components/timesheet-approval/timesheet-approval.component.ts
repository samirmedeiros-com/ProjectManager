import { Component, OnInit, ChangeDetectionStrategy, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { NavbarComponent } from '../navbar/navbar.component';
import { TimesheetService } from '../../services/timesheet.service';
import { ProjectService } from '../../services/project.service';
import { AuthService } from '../../services/auth.service';
import { ProjectUserCostService } from '../../services/project-user-cost.service';
import { UserService } from '../../services/user.service';
import { Timesheet, TimesheetListItem } from '../../models/timesheet.model';
import { Project } from '../../models/project.model';
import { User } from '../../models/user.model';

@Component({
  selector: 'app-timesheet-approval',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule, NavbarComponent],
  templateUrl: './timesheet-approval.component.html',
  styleUrls: ['./timesheet-approval.component.css'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class TimesheetApprovalComponent implements OnInit {
  projects: Project[] = [];
  pendingTimesheets: TimesheetListItem[] = [];
  approvedTimesheets: TimesheetListItem[] = [];
  currentTimesheet: Timesheet | null = null;
  rejectForm!: FormGroup;
  showRejectModal = false;
  showDeleteModal = false;
  isLoading = false;
  message = '';
  messageType: 'success' | 'error' | 'info' = 'info';
  userSetorId: number = 0;
  selectedMonth: string = new Date().toISOString().split('T')[0].slice(0, 7);
  statusFilter: 'pending' | 'approved' = 'pending';

  showCostsModal = false;
  costsForm!: FormGroup;
  users: User[] = [];
  selectedProjectId: number | null = null;
  selectedUserId: number | null = null;
  registeredCosts: any[] = [];

  constructor(
    private timesheetService: TimesheetService,
    private projectService: ProjectService,
    private authService: AuthService,
    private costService: ProjectUserCostService,
    private userService: UserService,
    private fb: FormBuilder,
    private cdr: ChangeDetectorRef,
    private router: Router
  ) {
    this.rejectForm = this.fb.group({
      reason: ['', [Validators.required, Validators.minLength(10)]]
    });
    this.costsForm = this.fb.group({
      costPerHour: ['', [Validators.required, Validators.min(0)]]
    });
  }

  ngOnInit(): void {
    // Get userSetorId from current user's setores
    const currentUser = this.authService.currentUserValue;
    console.log('Current User:', currentUser);
    console.log('User Role:', currentUser?.role);
    console.log('User Setores:', currentUser?.setores);

    if (!currentUser) {
      this.message = 'Usuário não autenticado. Faça login novamente.';
      this.messageType = 'error';
      this.cdr.markForCheck();
      return;
    }

    // Se é Admin, usa setorId = 0 (backend retorna todos)
    if (currentUser.role === 'Admin') {
      this.userSetorId = 0;
      console.log('Admin detected - loading all pending timesheets');
    }
    // Se é Gestor, usa o primeiro setor associado
    else if (currentUser.role === 'Gestor') {
      if (!currentUser.setores || currentUser.setores.length === 0) {
        this.message = 'Você não tem setores associados. Contacte o administrador.';
        this.messageType = 'error';
        this.cdr.markForCheck();
        return;
      }
      this.userSetorId = currentUser.setores[0].id;
      console.log('Gestor detected - using Setor ID:', this.userSetorId);
    }
    else {
      this.message = 'Você não tem permissão para aprovar timesheets.';
      this.messageType = 'error';
      this.cdr.markForCheck();
      return;
    }

    this.loadProjects();
    this.loadPendingApprovals();
    this.loadApprovedTimesheets();
  }

  onStatusFilterChange(): void {
    this.currentTimesheet = null;
    this.message = '';
    this.cdr.markForCheck();
  }

  loadProjects(): void {
    this.projectService.getAll().subscribe(
      (data: Project[]) => {
        this.projects = data;
      },
      (error: any) => {
        console.error('Erro ao carregar projetos:', error);
      }
    );
  }

  loadPendingApprovals(): void {
    this.isLoading = true;
    console.log('Loading pending approvals for setor:', this.userSetorId);

    this.timesheetService.getPendingApprovals(this.userSetorId).subscribe(
      (data) => {
        console.log('Pending timesheets:', data);
        this.pendingTimesheets = data;
        this.isLoading = false;
        this.cdr.markForCheck();
      },
      (error) => {
        console.error('Error loading pending approvals:', error);
        this.message = `Erro ao carregar timesheets: ${error.status} - ${error.statusText || error.message || 'Verifique se tem permissão'}`;
        this.messageType = 'error';
        this.isLoading = false;
        this.cdr.markForCheck();
      }
    );
  }

  loadApprovedTimesheets(): void {
    this.timesheetService.getMyTimesheets().subscribe(
      (data) => {
        this.approvedTimesheets = data.filter(ts => ts.status === 'Approved');
        this.cdr.markForCheck();
      },
      (error) => {
        console.error('Error loading approved timesheets:', error);
      }
    );
  }

  getFilteredTimesheets(): TimesheetListItem[] {
    const timesheets = this.statusFilter === 'pending' ? this.pendingTimesheets : this.approvedTimesheets;

    if (!this.selectedMonth) return timesheets;

    const [year, month] = this.selectedMonth.split('-');
    return timesheets.filter(ts => {
      const tsDate = new Date(ts.date);
      const tsYear = tsDate.getFullYear().toString();
      const tsMonth = String(tsDate.getMonth() + 1).padStart(2, '0');
      return tsYear === year && tsMonth === month;
    });
  }

  selectTimesheet(timesheetId: number): void {
    this.isLoading = true;
    this.timesheetService.getTimesheet(timesheetId).subscribe(
      (timesheet) => {
        // Enrich entries with project names from local projects array
        timesheet.entries = timesheet.entries.map(entry => ({
          ...entry,
          projectName: entry.projectName || this.projects.find(p => p.id === entry.projectId)?.name || 'Projeto'
        }));
        this.currentTimesheet = timesheet;
        this.isLoading = false;
        this.message = '';
        this.cdr.markForCheck();
      },
      (error) => {
        this.message = 'Erro ao carregar timesheet';
        this.messageType = 'error';
        this.isLoading = false;
        this.cdr.markForCheck();
      }
    );
  }

  approveTimesheet(): void {
    if (!this.currentTimesheet) return;

    this.isLoading = true;
    this.timesheetService.approveTimesheet(this.currentTimesheet.id).subscribe(
      () => {
        this.message = 'Timesheet aprovado com sucesso';
        this.messageType = 'success';
        this.currentTimesheet = null;
        this.loadPendingApprovals();
        this.isLoading = false;
        this.cdr.markForCheck();
      },
      (error) => {
        this.message = error.error?.message || 'Erro ao aprovar timesheet';
        this.messageType = 'error';
        this.isLoading = false;
        this.cdr.markForCheck();
      }
    );
  }

  openRejectModal(): void {
    this.showRejectModal = true;
    this.rejectForm.reset();
  }

  closeRejectModal(): void {
    this.showRejectModal = false;
    this.rejectForm.reset();
  }

  rejectTimesheet(): void {
    if (!this.currentTimesheet || this.rejectForm.invalid) return;

    const reason = this.rejectForm.get('reason')?.value;
    this.isLoading = true;

    this.timesheetService.rejectTimesheet(this.currentTimesheet.id, reason).subscribe(
      () => {
        this.message = 'Timesheet rejeitado com sucesso';
        this.messageType = 'success';
        this.currentTimesheet = null;
        this.showRejectModal = false;
        this.loadPendingApprovals();
        this.isLoading = false;
        this.cdr.markForCheck();
      },
      (error) => {
        this.message = error.error?.message || 'Erro ao rejeitar timesheet';
        this.messageType = 'error';
        this.isLoading = false;
        this.cdr.markForCheck();
      }
    );
  }

  getTotalHours(): number {
    return this.currentTimesheet?.entries.reduce((sum, entry) => sum + entry.workHours, 0) || 0;
  }

  decimalToTimeFormat(decimalHours: number): string {
    const hours = Math.floor(decimalHours);
    const minutes = Math.round((decimalHours - hours) * 60);
    return `${String(hours).padStart(2, '0')}:${String(minutes).padStart(2, '0')}`;
  }

  getStatusColor(status: string): string {
    switch (status) {
      case 'Draft': return '#ffc107';
      case 'Submitted': return '#17a2b8';
      case 'Approved': return '#28a745';
      case 'Rejected': return '#dc3545';
      default: return '#6c757d';
    }
  }

  goBack(): void {
    this.router.navigate(['/dashboard']);
  }

  goToReports(): void {
    this.router.navigate(['/reports']);
  }

  onMonthChange(): void {
    this.currentTimesheet = null;
    this.cdr.markForCheck();
  }

  openCostsModal(): void {
    this.showCostsModal = true;
    this.loadUsers();
    this.loadRegisteredCosts();
    this.costsForm.reset();
    this.selectedProjectId = null;
    this.selectedUserId = null;
    this.cdr.markForCheck();
  }

  closeCostsModal(): void {
    this.showCostsModal = false;
    this.costsForm.reset();
    this.cdr.markForCheck();
  }

  loadUsers(): void {
    this.userService.getAllUsers().subscribe(
      (data: User[]) => {
        this.users = data;
        this.cdr.markForCheck();
      },
      (error: any) => {
        console.error('Erro ao carregar utilizadores:', error);
      }
    );
  }

  loadRegisteredCosts(): void {
    if (!this.selectedProjectId && this.projects.length === 0) {
      this.registeredCosts = [];
      return;
    }

    // Carregar custos de todos os projetos se nenhum está selecionado
    if (!this.selectedProjectId && this.projects.length > 0) {
      // Carrega custos do primeiro projeto
      const projectId = this.projects[0].id;
      this.costService.getCostsByProject(projectId).subscribe(
        (data) => {
          this.registeredCosts = data;
          this.cdr.markForCheck();
        },
        (error: any) => {
          console.error('Erro ao carregar custos:', error);
          this.registeredCosts = [];
        }
      );
    }
  }

  onProjectChange(): void {
    this.loadRegisteredCosts();
  }

  saveCost(): void {
    if (!this.selectedProjectId || !this.selectedUserId || this.costsForm.invalid) {
      this.message = 'Selecione um projeto, utilizador e defina um custo';
      this.messageType = 'error';
      this.cdr.markForCheck();
      return;
    }

    const costPerHour = this.costsForm.get('costPerHour')?.value;
    this.isLoading = true;

    this.costService.createOrUpdateCost(this.selectedProjectId, this.selectedUserId, costPerHour).subscribe(
      () => {
        this.message = 'Custo configurado com sucesso';
        this.messageType = 'success';
        this.loadRegisteredCosts();
        this.costsForm.reset();
        this.selectedUserId = null;
        this.isLoading = false;
        this.cdr.markForCheck();
      },
      (error: any) => {
        this.message = 'Erro ao configurar custo';
        this.messageType = 'error';
        this.isLoading = false;
        this.cdr.markForCheck();
      }
    );
  }

  deleteCost(projectId: number, userId: number): void {
    if (!confirm('Tem a certeza que quer remover este custo?')) {
      return;
    }

    this.costService.deleteCost(projectId, userId).subscribe(
      () => {
        this.message = 'Custo removido com sucesso';
        this.messageType = 'success';
        this.loadRegisteredCosts();
        this.cdr.markForCheck();
      },
      (error: any) => {
        this.message = 'Erro ao remover custo';
        this.messageType = 'error';
        this.cdr.markForCheck();
      }
    );
  }

  deleteTimesheet(): void {
    if (!this.currentTimesheet) return;
    this.showDeleteModal = true;
    this.cdr.markForCheck();
  }

  closeDeleteModal(): void {
    this.showDeleteModal = false;
    this.cdr.markForCheck();
  }

  confirmDeleteTimesheet(): void {
    if (!this.currentTimesheet) return;

    this.isLoading = true;
    this.timesheetService.deleteTimesheet(this.currentTimesheet.id).subscribe(
      () => {
        this.message = 'Timesheet apagada com sucesso';
        this.messageType = 'success';
        this.isLoading = false;
        this.currentTimesheet = null;
        this.showDeleteModal = false;
        this.loadApprovedTimesheets();
        this.cdr.markForCheck();
      },
      (error: any) => {
        this.message = 'Erro ao apagar timesheet';
        this.messageType = 'error';
        this.isLoading = false;
        this.showDeleteModal = false;
        this.cdr.markForCheck();
      }
    );
  }
}
