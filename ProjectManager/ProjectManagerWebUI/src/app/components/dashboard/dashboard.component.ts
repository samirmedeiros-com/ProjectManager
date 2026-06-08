import { Component, OnInit, ChangeDetectionStrategy, ChangeDetectorRef } from '@angular/core';
import { Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AuthService } from '../../services/auth.service';
import { UserService } from '../../services/user.service';
import { ProjectService } from '../../services/project.service';
import { SetorService } from '../../services/setor.service';
import { ChangePasswordComponent } from '../change-password/change-password.component';
import { Project, CreateProjectRequest } from '../../models/project.model';
import { User } from '../../models/user.model';
import jsPDF from 'jspdf';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, FormsModule, ChangePasswordComponent],
  // ChangePasswordComponent importado para usar no template
  templateUrl: './dashboard.component.html',
  styleUrls: ['./dashboard.component.css'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class DashboardComponent implements OnInit {
  projects: Project[] = [];
  users: User[] = [];
  allUsers: User[] = [];
  setores: any[] = [];
  gestorSetores: any[] = [];
  loading = false;
  loadingUsers = false;
  loadingSetores = false;
  showNewProjectForm = false;
  showUserMenu = false;

  get currentUser(): User | null {
    return this.authService.currentUserValue;
  }

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

  // Edição inline
  editingManagerId: number | null = null;
  editingStatusId: number | null = null;
  editingManagerValue = '';
  editingStatusValue = '';
  savingManagerId: number | null = null;
  savingStatusId: number | null = null;

  newProjectForm = {
    name: '',
    description: '',
    startDate: '',
    endDate: '',
    priority: 1,
    ownerId: null as number | null,
    setorId: null as number | null
  };

  filterByResponsavel = '';
  filterByStatus = '';
  filterBySetor: number | string = '';

  statusOptions = [
    { value: 'Planning', label: 'Planeamento' },
    { value: 'Released', label: 'Por Iniciar' },
    { value: 'Development', label: 'Desenvolvimento' },
    { value: 'Completed', label: 'Concluído' },
    { value: 'On Hold', label: 'Em Espera' },
    { value: 'Finished', label: 'Finalizado' }
  ];


  constructor(
    private authService: AuthService,
    private userService: UserService,
    private projectService: ProjectService,
    private setorService: SetorService,
    private router: Router,
    private cdr: ChangeDetectorRef
  ) { }

  ngOnInit() {
    this.loadProjects();
    this.loadSetores();
    this.loadAllUsers(); // Carrega todos os utilizadores para o dropdown de Owner
    this.loadGestorSetores(); // Carrega setores do gestor para o formulário de criação

    // Apenas carrega utilizadores se for Admin ou Gestor (para o filtro de responsável)
    if (this.isGestor()) {
      this.loadUsers();
    }

    // Se não é gestor, filtra automaticamente pelos seus projetos
    if (!this.isGestor() && this.currentUser?.email) {
      this.filterByResponsavel = this.currentUser.email;
    }
  }

  loadSetores() {
    this.setorService.getAllSetores().subscribe({
      next: (data) => {
        this.setores = data;
        this.cdr.markForCheck();
      },
      error: (error) => {
        console.error('Erro ao carregar setores:', error);
      }
    });
  }

  loadGestorSetores() {
    // Carrega apenas os setores do gestor atual (para usar no formulário de criação)
    console.log('[Dashboard] Chamando loadGestorSetores...');
    this.projectService.getUserSectors().subscribe({
      next: (data) => {
        console.log('[Dashboard] Setores do gestor carregados:', data);
        this.gestorSetores = data;
        this.cdr.markForCheck();
      },
      error: (error) => {
        console.error('[Dashboard] Erro ao carregar setores do gestor:', error);
      }
    });
  }

  loadUsers() {
    this.loadingUsers = true;
    this.userService.getAllUsers().subscribe({
      next: (data) => {
        this.users = data;
        this.loadingUsers = false;
        this.cdr.markForCheck();
      },
      error: (error) => {
        console.error('Erro ao carregar utilizadores:', error);
        this.loadingUsers = false;
        this.cdr.markForCheck();
      }
    });
  }

  loadAllUsers() {
    // Carrega todos os utilizadores para o dropdown de Owner (não filtrado)
    this.userService.getAllUsersUnfiltered().subscribe({
      next: (data) => {
        this.allUsers = data;
        this.cdr.markForCheck();
      },
      error: (error) => {
        console.error('Erro ao carregar todos os utilizadores:', error);
      }
    });
  }

  loadProjects() {
    this.loading = true;
    this.cdr.markForCheck();

    this.projectService.getAll().subscribe(
      (data) => {
        console.log('[Dashboard] Projetos carregados:', data.length, 'projetos');
        this.projects = data;
        console.log('[Dashboard] Filtrados:', this.getFilteredProjects().length, 'projetos');
        this.updateCharts();
        this.loading = false;
        this.cdr.markForCheck();
      },
      (error) => {
        console.error('[DashboardComponent] Erro ao carregar projetos:', error.status, error.message);
        this.loading = false;
        this.cdr.markForCheck();
      }
    );
  }

  viewProject(projectId: number) {
    this.router.navigate(['/projects', projectId]);
  }

  goToSetores() {
    this.router.navigate(['/setores']);
  }

  goToUsers() {
    this.router.navigate(['/users']);
  }

  openFreshDeskTicket(freshDeskId: string | undefined) {
    if (freshDeskId) {
      const url = `https://suporte.dpd.pt/a/tickets/${freshDeskId}`;
      window.open(url, '_blank');
    }
  }

  logout() {
    this.authService.logout();
    this.router.navigate(['/login']);
  }

  newProject() {
    this.showNewProjectForm = !this.showNewProjectForm;
  }

  trackByProjectId(index: number, project: Project): number {
    return project.id;
  }

  createProject() {
    if (!this.newProjectForm.name || !this.newProjectForm.startDate) {
      alert('Por favor, preencha os campos obrigatórios (Nome e Data de Início)');
      return;
    }

    const request: CreateProjectRequest = {
      name: this.newProjectForm.name,
      description: this.newProjectForm.description,
      startDate: new Date(this.newProjectForm.startDate),
      endDate: this.newProjectForm.endDate ? new Date(this.newProjectForm.endDate) : undefined,
      priority: this.newProjectForm.priority,
      manager: this.currentUser?.email || 'Desconhecido',
      ownerId: this.newProjectForm.ownerId,
      setorId: this.newProjectForm.setorId
    };

    this.projectService.create(request).subscribe(
      (newProject) => {
        this.projects = [...this.projects, newProject];
        this.showNewProjectForm = false;
        this.resetNewProjectForm();
        this.cdr.markForCheck();
      },
      (error) => {
        console.error('Erro ao criar projeto:', error.message);
        alert('Erro ao criar projeto: ' + error.message);
      }
    );
  }

  private resetNewProjectForm() {
    this.newProjectForm = {
      name: '',
      description: '',
      startDate: '',
      endDate: '',
      priority: 1,
      ownerId: null,
      setorId: null
    };
  }

  isGestor(): boolean {
    const role = this.currentUser?.role;
    return role === 'Admin' || role === 'Gestor';
  }

  isOwner(): boolean {
    return this.currentUser?.role === 'Owner';
  }

  isResponsavel(project: Project): boolean {
    return project.manager === this.currentUser?.email;
  }

  isOnlyOwner(project: Project): boolean {
    // Apenas owner se é owner E NÃO é responsável
    const isOwner = project.ownerId === this.currentUser?.id;
    const isResponsavel = this.isResponsavel(project);
    return isOwner && !isResponsavel;
  }

  onFilterChange() {
    this.cdr.markForCheck();
  }

  getStatusColor(statusValue: string): string {
    const colors: { [key: string]: string } = {
      'Planning': '#DC0032',
      'Released': '#1976d2',
      'Development': '#FF9800',
      'Completed': '#4CAF50',
      'On Hold': '#FFC107',
      'Finished': '#9C27B0'
    };
    return colors[statusValue] || '#999999';
  }

  getStatusLabel(statusValue: string): string {
    const status = this.statusOptions.find(s => s.value === statusValue);
    return status ? status.label : statusValue;
  }

  canChangeStatus(currentStatus: string, project?: Project): boolean {
    if (this.currentUser?.role === 'Admin') return true;
    if (this.isGestor()) return true;

    // Owners-apenas não podem alterar status
    if (project && this.isOnlyOwner(project)) {
      return false;
    }

    // Utilizadores podem apenas mudar: Released → Development, Development → Completed
    return currentStatus === 'Released' || currentStatus === 'Development';
  }

  getAllowedStatusOptions(currentStatus: string): any[] {
    if (this.isGestor()) return this.statusOptions;

    // Transições permitidas para utilizadores
    if (currentStatus === 'Released') {
      return this.statusOptions.filter(s => s.value === 'Development');
    } else if (currentStatus === 'Development') {
      return this.statusOptions.filter(s => s.value === 'Completed');
    }

    return [];
  }

  private updateCharts() {
    this.cdr.markForCheck();
  }

  getStatusSummary(): any[] {
    const colors: { [key: string]: string } = {
      'Planning': '#DC0032',
      'Released': '#1976d2',
      'Development': '#FF9800',
      'Completed': '#4CAF50',
      'On Hold': '#FFC107',
      'Finished': '#9C27B0'
    };

    const setorData: { [setorId: number | string]: { setorName: string; statuses: { [key: string]: number } } } = {};

    // Se há filtro de setor, mostrar apenas esse setor
    if (this.filterBySetor) {
      const setorId = parseInt(this.filterBySetor.toString());
      const setor = this.gestorSetores.find(s => s.id === setorId);
      if (setor) {
        setorData[setor.id] = { setorName: setor.name, statuses: {} };
        this.statusOptions.forEach(s => setorData[setor.id].statuses[s.value] = 0);
      }
    } else {
      // Usar setores do gestor (ou todos se for Admin sem filtro)
      const setoresParaGrafico = this.gestorSetores.length > 0 ? this.gestorSetores : this.setores;

      // Inicializar setores
      setoresParaGrafico.forEach(setor => {
        setorData[setor.id] = { setorName: setor.name, statuses: {} };
        this.statusOptions.forEach(s => setorData[setor.id].statuses[s.value] = 0);
      });

      // Inicializar "Sem Setor" para projetos sem setor atribuído
      setorData['without-sector'] = { setorName: 'Sem Setor', statuses: {} };
      this.statusOptions.forEach(s => setorData['without-sector'].statuses[s.value] = 0);
    }

    // Usar projetos filtrados (considera filtros de setor, responsável, status)
    const filteredProjects = this.getFilteredProjects();

    // Contar projetos por setor
    filteredProjects.forEach(project => {
      if (project.setorId && setorData[project.setorId]) {
        if (setorData[project.setorId].statuses[project.status] !== undefined) {
          setorData[project.setorId].statuses[project.status]++;
        }
      } else if (!project.setorId) {
        // Contar projetos sem setor
        if (setorData['without-sector'].statuses[project.status] !== undefined) {
          setorData['without-sector'].statuses[project.status]++;
        }
      }
    });

    // Converter para array com dados formatados, filtrando setores sem projetos
    return Object.entries(setorData)
      .map(([setorId, data]) => {
        const total = Object.values(data.statuses).reduce((a, b) => a + b, 0);
        return {
          setorName: data.setorName,
          total: total,
          items: this.statusOptions.map(s => {
            const count = data.statuses[s.value] || 0;
            return {
              label: s.label,
              count,
              percentage: total > 0 ? (count / total) * 100 : 0,
              color: colors[s.value]
            };
          })
        };
      })
      .filter(setor => setor.total > 0);
  }

  getPrioritySummary(): any[] {
    const colors = ['#00994d', '#66BB6A', '#FBC02D', '#F57C00', '#D32F2F'];

    const setorData: { [setorId: number | string]: { setorName: string; priorities: number[] } } = {};

    // Se há filtro de setor, mostrar apenas esse setor
    if (this.filterBySetor) {
      const setorId = parseInt(this.filterBySetor.toString());
      const setor = this.gestorSetores.find(s => s.id === setorId);
      if (setor) {
        setorData[setor.id] = { setorName: setor.name, priorities: [0, 0, 0, 0, 0] };
      }
    } else {
      // Usar setores do gestor (ou todos se for Admin sem filtro)
      const setoresParaGrafico = this.gestorSetores.length > 0 ? this.gestorSetores : this.setores;

      // Inicializar setores
      setoresParaGrafico.forEach(setor => {
        setorData[setor.id] = { setorName: setor.name, priorities: [0, 0, 0, 0, 0] };
      });

      // Inicializar "Sem Setor" para projetos sem setor atribuído
      setorData['without-sector'] = { setorName: 'Sem Setor', priorities: [0, 0, 0, 0, 0] };
    }

    // Usar projetos filtrados (considera filtros de setor, responsável, status)
    const filteredProjects = this.getFilteredProjects();

    // Contar projetos por setor
    filteredProjects.forEach(project => {
      if (project.setorId && setorData[project.setorId]) {
        if (project.priority >= 1 && project.priority <= 5) {
          setorData[project.setorId].priorities[project.priority - 1]++;
        }
      } else if (!project.setorId && project.priority >= 1 && project.priority <= 5) {
        // Contar projetos sem setor
        setorData['without-sector'].priorities[project.priority - 1]++;
      }
    });

    // Converter para array com dados formatados, filtrando setores sem projetos
    return Object.entries(setorData)
      .map(([setorId, data]) => {
        const total = data.priorities.reduce((a, b) => a + b, 0);
        return {
          setorName: data.setorName,
          total: total,
          items: data.priorities.map((count, idx) => {
            return {
              label: `Prioridade ${idx + 1}`,
              count,
              percentage: total > 0 ? (count / total) * 100 : 0,
              color: colors[idx]
            };
          })
        };
      })
      .filter(setor => setor.total > 0);
  }

  getDeadlineSummary(): any[] {
    const colors: { [key: string]: string } = {
      'Concluído': '#4CAF50',
      'No Prazo': '#00994d',
      'Próximo': '#FFC107',
      'Atrasado': '#D32F2F',
      'Não Iniciado': '#9C27B0'
    };

    const setorData: { [setorId: number | string]: { setorName: string; deadlines: { [key: string]: number } } } = {};

    // Se há filtro de setor, mostrar apenas esse setor
    if (this.filterBySetor) {
      const setorId = parseInt(this.filterBySetor.toString());
      const setor = this.gestorSetores.find(s => s.id === setorId);
      if (setor) {
        setorData[setor.id] = {
          setorName: setor.name,
          deadlines: {
            'Concluído': 0,
            'No Prazo': 0,
            'Próximo': 0,
            'Atrasado': 0,
            'Não Iniciado': 0
          }
        };
      }
    } else {
      // Usar setores do gestor (ou todos se for Admin sem filtro)
      const setoresParaGrafico = this.gestorSetores.length > 0 ? this.gestorSetores : this.setores;

      // Inicializar setores
      setoresParaGrafico.forEach(setor => {
        setorData[setor.id] = {
          setorName: setor.name,
          deadlines: {
            'Concluído': 0,
            'No Prazo': 0,
            'Próximo': 0,
            'Atrasado': 0,
            'Não Iniciado': 0
          }
        };
      });

      // Inicializar "Sem Setor" para projetos sem setor atribuído
      setorData['without-sector'] = {
        setorName: 'Sem Setor',
        deadlines: {
          'Concluído': 0,
          'No Prazo': 0,
          'Próximo': 0,
          'Atrasado': 0,
          'Não Iniciado': 0
        }
      };
    }

    // Usar projetos filtrados (considera filtros de setor, responsável, status)
    const filteredProjects = this.getFilteredProjects();

    // Contar projetos por setor
    filteredProjects.forEach(project => {
      if (project.setorId && setorData[project.setorId]) {
        const deadline = this.getDeadlineStatus(project);
        if (deadline.text.includes('Concluído')) setorData[project.setorId].deadlines['Concluído']++;
        else if (deadline.text.includes('Próximo') || deadline.text.includes('Entrega')) setorData[project.setorId].deadlines['Próximo']++;
        else if (deadline.text.includes('Atrasado')) setorData[project.setorId].deadlines['Atrasado']++;
        else if (deadline.text.includes('Inicia em')) setorData[project.setorId].deadlines['Não Iniciado']++;
        else setorData[project.setorId].deadlines['No Prazo']++;
      } else if (!project.setorId) {
        // Contar projetos sem setor
        const deadline = this.getDeadlineStatus(project);
        if (deadline.text.includes('Concluído')) setorData['without-sector'].deadlines['Concluído']++;
        else if (deadline.text.includes('Próximo') || deadline.text.includes('Entrega')) setorData['without-sector'].deadlines['Próximo']++;
        else if (deadline.text.includes('Atrasado')) setorData['without-sector'].deadlines['Atrasado']++;
        else if (deadline.text.includes('Inicia em')) setorData['without-sector'].deadlines['Não Iniciado']++;
        else setorData['without-sector'].deadlines['No Prazo']++;
      }
    });

    // Converter para array com dados formatados, filtrando setores sem projetos
    return Object.entries(setorData)
      .map(([setorId, data]) => {
        const total = Object.values(data.deadlines).reduce((a, b) => a + b, 0);
        return {
          setorName: data.setorName,
          total: total,
          items: Object.entries(data.deadlines).map(([label, count]) => {
            return {
              label,
              count,
              percentage: total > 0 ? (count / total) * 100 : 0,
              color: colors[label]
            };
          })
        };
      })
      .filter(setor => setor.total > 0);
  }

  getManagerSummary(): any[] {
    const colors: { [key: string]: string } = {
      'Planning': '#DC0032',
      'Released': '#1976d2',
      'Development': '#FF9800',
      'Completed': '#4CAF50',
      'On Hold': '#FFC107',
      'Finished': '#9C27B0'
    };

    const filteredProjects = this.getFilteredProjects();
    const managerData: { [email: string]: { name: string; statuses: { [key: string]: number } } } = {};

    // Inicializar managers com contadores de status
    filteredProjects.forEach(project => {
      if (project.manager) {
        if (!managerData[project.manager]) {
          managerData[project.manager] = {
            name: project.managerName || project.manager,
            statuses: {}
          };
          this.statusOptions.forEach(s => managerData[project.manager].statuses[s.value] = 0);
        }
      }
    });

    // Contar projetos por status para cada manager
    filteredProjects.forEach(project => {
      if (project.manager && managerData[project.manager]) {
        if (managerData[project.manager].statuses[project.status] !== undefined) {
          managerData[project.manager].statuses[project.status]++;
        }
      }
    });

    // Converter para array com dados formatados (mesmo estilo que getStatusSummary)
    return Object.entries(managerData)
      .map(([email, data]) => {
        const total = Object.values(data.statuses).reduce((a, b) => a + b, 0);
        return {
          email,
          name: data.name,
          total: total,
          items: this.statusOptions.map(s => {
            const count = data.statuses[s.value] || 0;
            return {
              label: s.label,
              count,
              percentage: total > 0 ? (count / total) * 100 : 0,
              color: colors[s.value]
            };
          })
        };
      })
      .filter(manager => manager.total > 0)
      .sort((a, b) => b.total - a.total);
  }

  getOwnerSummary(): any[] {
    const colors: { [key: string]: string } = {
      'Planning': '#DC0032',
      'Released': '#1976d2',
      'Development': '#FF9800',
      'Completed': '#4CAF50',
      'On Hold': '#FFC107',
      'Finished': '#9C27B0'
    };

    const filteredProjects = this.getFilteredProjects();
    const ownerData: { [id: number]: { name: string; statuses: { [key: string]: number } } } = {};
    const noOwnerStatuses: { [key: string]: number } = {};

    // Inicializar contadores de status
    this.statusOptions.forEach(s => noOwnerStatuses[s.value] = 0);

    // Inicializar owners com contadores de status
    filteredProjects.forEach(project => {
      if (project.ownerId) {
        const ownerId = project.ownerId;
        if (!ownerData[ownerId]) {
          ownerData[ownerId] = {
            name: project.ownerName || 'Desconhecido',
            statuses: {}
          };
          this.statusOptions.forEach(s => ownerData[ownerId].statuses[s.value] = 0);
        }
      }
    });

    // Contar projetos por status para cada owner
    filteredProjects.forEach(project => {
      if (project.ownerId && ownerData[project.ownerId]) {
        if (ownerData[project.ownerId].statuses[project.status] !== undefined) {
          ownerData[project.ownerId].statuses[project.status]++;
        }
      } else if (!project.ownerId) {
        // Contar projetos sem owner
        if (noOwnerStatuses[project.status] !== undefined) {
          noOwnerStatuses[project.status]++;
        }
      }
    });

    // Converter para array com dados formatados (mesmo estilo que getStatusSummary)
    const result = Object.entries(ownerData)
      .map(([id, data]) => {
        const total = Object.values(data.statuses).reduce((a, b) => a + b, 0);
        return {
          id: parseInt(id),
          name: data.name,
          total: total,
          items: this.statusOptions.map(s => {
            const count = data.statuses[s.value] || 0;
            return {
              label: s.label,
              count,
              percentage: total > 0 ? (count / total) * 100 : 0,
              color: colors[s.value]
            };
          })
        };
      })
      .filter(owner => owner.total > 0)
      .sort((a, b) => b.total - a.total);

    // Adicionar "Sem Proprietário" se houver projetos sem owner
    const noOwnerTotal = Object.values(noOwnerStatuses).reduce((a, b) => a + b, 0);
    if (noOwnerTotal > 0) {
      result.push({
        id: 0,
        name: 'Sem Proprietário',
        total: noOwnerTotal,
        items: this.statusOptions.map(s => {
          const count = noOwnerStatuses[s.value] || 0;
          return {
            label: s.label,
            count,
            percentage: noOwnerTotal > 0 ? (count / noOwnerTotal) * 100 : 0,
            color: colors[s.value]
          };
        })
      });
    }

    return result;
  }

  getFilteredProjects(): Project[] {
    let filtered = this.projects;

    // Se é gestor, mostrar todos os projetos (ou filtrar por responsável se selecionado)
    if (this.isGestor()) {
      if (this.filterByResponsavel) {
        filtered = filtered.filter(p => p.manager === this.filterByResponsavel);
      }
    } else {
      // Se é utilizador normal, mostrar apenas projetos onde é responsável OU owner
      filtered = filtered.filter(project => {
        const isResponsavel = project.manager === this.currentUser?.email;
        const isOwner = project.ownerId === this.currentUser?.id;
        return isResponsavel || isOwner;
      });
    }

    // Aplicar filtro de status se existir
    if (this.filterByStatus) {
      filtered = filtered.filter(p => p.status === this.filterByStatus);
    }

    // Aplicar filtro de setor se existir
    if (this.filterBySetor) {
      const setorId = parseInt(this.filterBySetor.toString());
      filtered = filtered.filter(p => p.setorId === setorId);
    }

    // Ordenar por prioridade (maior para menor) e depois por data de conclusão (mais próxima para mais longe)
    return filtered.sort((a, b) => {
      // Primeiro por prioridade (maior número primeiro)
      if (a.priority !== b.priority) {
        return b.priority - a.priority;
      }

      // Se prioridade é igual, ordenar por data de término
      // Projetos sem data de término vão para o final
      if (!a.endDate && !b.endDate) return 0;
      if (!a.endDate) return 1;
      if (!b.endDate) return -1;

      const dateA = new Date(a.endDate).getTime();
      const dateB = new Date(b.endDate).getTime();
      return dateA - dateB;
    });
  }

  getDeadlineStatus(project: Project): { text: string; class: string } {
    const today = new Date();
    today.setHours(0, 0, 0, 0);

    const startDate = new Date(project.startDate);
    startDate.setHours(0, 0, 0, 0);

    const endDate = project.endDate ? new Date(project.endDate) : null;
    if (endDate) {
      endDate.setHours(0, 0, 0, 0);
    }

    // Se já foi concluído
    if (project.status === 'Completed') {
      return { text: '✅ Concluído', class: 'deadline-completed' };
    }

    // Se ainda não iniciou
    if (today < startDate) {
      const daysUntilStart = Math.ceil((startDate.getTime() - today.getTime()) / (1000 * 60 * 60 * 24));
      return { text: `⏰ Inicia em ${daysUntilStart}d`, class: 'deadline-pending' };
    }

    // Se passou a data de início mas não finalizou
    if (today >= startDate && project.status === 'Planning') {
      return { text: '🔴 Início Atrasado', class: 'deadline-overdue' };
    }

    // Se tem data de fim
    if (endDate) {
      const daysUntilEnd = Math.ceil((endDate.getTime() - today.getTime()) / (1000 * 60 * 60 * 24));

      // Passou a data de entrega
      if (today > endDate) {
        return { text: `🔴 ${Math.abs(daysUntilEnd)}d Atrasado`, class: 'deadline-overdue' };
      }

      // Prazo próximo (menos de 7 dias)
      if (daysUntilEnd <= 7 && daysUntilEnd > 0) {
        return { text: `🟡 Entrega em ${daysUntilEnd}d`, class: 'deadline-warning' };
      }

      // No prazo
      return { text: `✓ ${daysUntilEnd}d restante`, class: 'deadline-ontrack' };
    }

    // Sem data de fim
    return { text: '✓ Em andamento', class: 'deadline-ontrack' };
  }

  // Edição de Responsável
  editManager(projectId: number, currentManager: string) {
    this.editingManagerId = projectId;
    this.editingManagerValue = currentManager;
    if (this.users.length === 0 && !this.loadingUsers) {
      this.loadUsers();
    }
  }

  cancelEditManager() {
    this.editingManagerId = null;
    this.editingManagerValue = '';
  }

  saveManager(projectId: number) {
    if (!this.editingManagerValue.trim()) {
      this.editingManagerValue = '';
      return;
    }

    // Se o valor é um email válido, usa direto. Se é um ID, busca o email
    let managerEmail = this.editingManagerValue.trim();

    // Verifica se é um email
    if (!managerEmail.includes('@')) {
      // É um ID ou nome, tenta encontrar na lista de utilizadores
      const user = this.users.find(u => u.id.toString() === managerEmail || u.fullName === managerEmail);
      if (user) {
        managerEmail = user.email;
      }
    }

    this.savingManagerId = projectId;
    this.cdr.markForCheck();

    this.projectService.updateManager(projectId, managerEmail).subscribe({
      next: (updatedProject) => {
        const index = this.projects.findIndex(p => p.id === projectId);
        if (index !== -1) {
          this.projects[index] = updatedProject;
          this.projects = [...this.projects];
        }
        this.editingManagerId = null;
        this.editingManagerValue = '';
        this.savingManagerId = null;
        this.cdr.markForCheck();
      },
      error: (error) => {
        console.error('Erro ao atualizar responsável:', error);
        this.savingManagerId = null;
        this.cdr.markForCheck();
      }
    });
  }

  // Edição de Status
  editStatus(projectId: number, currentStatus: string) {
    this.editingStatusId = projectId;
    this.editingStatusValue = currentStatus || '';
    this.cdr.markForCheck();
  }

  cancelEditStatus() {
    this.editingStatusId = null;
    this.editingStatusValue = '';
    this.cdr.markForCheck();
  }

  saveStatus(projectId: number) {
    const statusValue = this.editingStatusValue ? this.editingStatusValue.trim() : '';

    if (!statusValue) {
      return;
    }

    this.savingStatusId = projectId;
    this.cdr.markForCheck();

    this.projectService.updateStatus(projectId, statusValue).subscribe({
      next: () => {
        this.editingStatusId = null;
        this.editingStatusValue = '';
        this.savingStatusId = null;
        this.loadProjects();
      },
      error: (error) => {
        this.savingStatusId = null;
        this.cdr.markForCheck();
      }
    });
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

  async generateProjectsPDF() {
    try {
      const filteredProjects = this.getFilteredProjects();

      if (filteredProjects.length === 0) {
        alert('Nenhum projeto para exportar com os filtros aplicados');
        return;
      }

      const doc = new jsPDF({
        orientation: 'landscape',
        unit: 'mm',
        format: 'a4'
      });

      const pageWidth = doc.internal.pageSize.getWidth();
      const pageHeight = doc.internal.pageSize.getHeight();
      const margin = 10;
      let yPosition = margin;

      // Desenhar logo DPD simplificada
      doc.setFillColor(220, 0, 50);
      doc.rect(margin, yPosition, 8, 8, 'F');

      doc.setFont('Helvetica', 'bold');
      doc.setFontSize(7);
      doc.setTextColor(255, 255, 255);
      doc.text('DPD', margin + 1.5, yPosition + 5.5);

      doc.setFont('Helvetica', 'bold');
      doc.setFontSize(14);
      doc.setTextColor(220, 0, 50);
      doc.text('Relatório de Projetos', margin + 11, yPosition + 4);

      doc.setFont('Helvetica', 'normal');
      doc.setFontSize(9);
      doc.setTextColor(100, 100, 100);
      yPosition += 10;
      doc.text(`Data: ${new Date().toLocaleDateString('pt-PT')} | Utilizador: ${this.currentUser?.fullName || 'Desconhecido'}`, margin, yPosition);

      yPosition += 8;
      doc.setDrawColor(220, 0, 50);
      doc.line(margin, yPosition, pageWidth - margin, yPosition);

      yPosition += 5;
      doc.setFont('Helvetica', 'bold');
      doc.setFontSize(8);
      doc.setTextColor(80, 80, 80);

      if (this.filterByResponsavel || this.filterByStatus) {
        doc.text('Filtros Aplicados:', margin, yPosition);
        yPosition += 4;
        doc.setFont('Helvetica', 'normal');
        doc.setFontSize(7);

        if (this.filterByResponsavel) {
          const responsavelName = this.users.find(u => u.email === this.filterByResponsavel)?.fullName || this.filterByResponsavel;
          doc.text(`  • Responsável: ${responsavelName}`, margin + 3, yPosition);
          yPosition += 3;
        }

        if (this.filterByStatus) {
          const statusLabel = this.getStatusLabel(this.filterByStatus);
          doc.text(`  • Status: ${statusLabel}`, margin + 3, yPosition);
          yPosition += 3;
        }

        yPosition += 2;
      }

      yPosition += 2;

      const tableColumns = [
        { header: 'Projeto', dataKey: 'name', width: 50 },
        { header: 'Descrição', dataKey: 'description', width: 45 },
        { header: 'Responsável', dataKey: 'managerName', width: 32 },
        { header: 'Status', dataKey: 'status', width: 25 },
        { header: 'Prioridade', dataKey: 'priority', width: 15 },
        { header: 'Início', dataKey: 'startDate', width: 20 },
        { header: 'Término', dataKey: 'endDate', width: 20 }
      ];

      const tableData = filteredProjects.map(project => ({
        name: project.name,
        description: project.description || '-',
        managerName: project.managerName || 'Desconhecido',
        status: this.getStatusLabel(project.status),
        priority: `★ ${project.priority}`,
        startDate: new Date(project.startDate).toLocaleDateString('pt-PT'),
        endDate: project.endDate ? new Date(project.endDate).toLocaleDateString('pt-PT') : '-'
      }));

      doc.setFont('Helvetica', 'normal');
      doc.setFontSize(9);
      doc.setTextColor(0, 0, 0);

      let currentY = yPosition;
      const rowHeight = 8;
      const headerHeight = 10;

      doc.setFont('Helvetica', 'bold');
      doc.setFillColor(220, 0, 50);
      doc.setTextColor(255, 255, 255);
      doc.setFontSize(10);

      let headerX = margin;
      tableColumns.forEach(col => {
        doc.rect(headerX, currentY, col.width, headerHeight, 'F');
        // Centralizar o texto verticalmente no cabeçalho
        doc.text(col.header, headerX + col.width / 2, currentY + 6.5, {
          maxWidth: col.width - 2,
          align: 'center'
        });
        headerX += col.width;
      });

      currentY += headerHeight;
      doc.setFont('Helvetica', 'normal');
      doc.setTextColor(0, 0, 0);
      doc.setFontSize(8);

      let rowColor = false;
      tableData.forEach((row, index) => {
        if (currentY + rowHeight > pageHeight - margin - 5) {
          doc.addPage();
          currentY = margin;

          // Repetir cabeçalho em nova página
          doc.setFont('Helvetica', 'bold');
          doc.setFillColor(220, 0, 50);
          doc.setTextColor(255, 255, 255);
          doc.setFontSize(10);

          let headerX = margin;
          tableColumns.forEach(col => {
            doc.rect(headerX, currentY, col.width, headerHeight, 'F');
            doc.text(col.header, headerX + col.width / 2, currentY + 6.5, {
              maxWidth: col.width - 2,
              align: 'center'
            });
            headerX += col.width;
          });

          currentY += headerHeight;
          doc.setFont('Helvetica', 'normal');
          doc.setTextColor(0, 0, 0);
          doc.setFontSize(8);
          rowColor = false;
        }

        if (rowColor) {
          doc.setFillColor(245, 245, 245);
          let fillX = margin;
          tableColumns.forEach(col => {
            doc.rect(fillX, currentY, col.width, rowHeight, 'F');
            fillX += col.width;
          });
        }

        let cellX = margin;
        tableColumns.forEach((col, colIndex) => {
          const cellValue = (row as any)[col.dataKey] || '';
          const displayValue = colIndex === 1 ? cellValue.substring(0, 40) : cellValue.toString();
          doc.text(displayValue, cellX + 2, currentY + 5, { maxWidth: col.width - 4 });
          cellX += col.width;
        });

        doc.setDrawColor(220, 220, 220);
        let lineX = margin;
        tableColumns.forEach(col => {
          doc.line(lineX, currentY + rowHeight, lineX, currentY);
          lineX += col.width;
        });
        doc.line(margin, currentY + rowHeight, pageWidth - margin, currentY + rowHeight);

        currentY += rowHeight;
        rowColor = !rowColor;
      });

      // Rodapé com número de página e informações adicionais
      const pageCount = doc.getNumberOfPages();
      for (let i = 1; i <= pageCount; i++) {
        doc.setPage(i);
        doc.setFont('Helvetica', 'normal');
        doc.setFontSize(7);
        doc.setTextColor(150, 150, 150);
        doc.text(
          `Página ${i} de ${pageCount} | Gerado em ${new Date().toLocaleString('pt-PT')}`,
          margin,
          pageHeight - 5
        );
      }

      const fileName = `Relatorio_Projetos_${new Date().toISOString().slice(0, 19).replace(/:/g, '-')}.pdf`;
      doc.save(fileName);
    } catch (error) {
      console.error('Erro ao gerar PDF:', error);
      alert('Erro ao gerar PDF. Tente novamente.');
    }
  }
}
