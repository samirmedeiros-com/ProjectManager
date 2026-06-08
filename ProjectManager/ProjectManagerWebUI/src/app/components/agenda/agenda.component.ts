import { Component, OnInit, ChangeDetectionStrategy, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { NavbarComponent } from '../navbar/navbar.component';
import { EventService } from '../../services/event.service';
import { ProjectService } from '../../services/project.service';
import { Event, CreateEventRequest } from '../../models/event.model';
import { Project } from '../../models/project.model';

@Component({
  selector: 'app-agenda',
  standalone: true,
  imports: [CommonModule, FormsModule, NavbarComponent],
  templateUrl: './agenda.component.html',
  styleUrls: ['./agenda.component.css'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class AgendaComponent implements OnInit {
  currentDate: Date = new Date();
  selectedDate: Date = new Date();
  isLoading = false;
  events: Event[] = [];
  projects: Project[] = [];

  showEventModal = false;
  eventForm = {
    title: '',
    description: '',
    startTime: '09:00',
    endTime: '10:00',
    projectId: null as number | null,
    isApplicableToProject: false
  };
  isEditingEvent = false;
  editingEventId: number | null = null;

  showAlertModal = false;
  alertData = {
    title: '',
    message: '',
    type: 'info', // 'info', 'error', 'confirm'
    confirmAction: () => {},
    confirmText: 'OK',
    cancelText: 'Cancelar'
  };

  constructor(
    private router: Router,
    private cdr: ChangeDetectorRef,
    private eventService: EventService,
    private projectService: ProjectService
  ) {}

  ngOnInit(): void {
    this.loadProjects();
    this.loadEvents();
  }

  loadEvents(): void {
    const firstDay = new Date(this.currentDate.getFullYear(), this.currentDate.getMonth(), 1);
    const lastDay = new Date(this.currentDate.getFullYear(), this.currentDate.getMonth() + 1, 0);

    this.eventService.getUserEvents(firstDay, lastDay).subscribe({
      next: (data: Event[]) => {
        this.events = data;
        this.cdr.markForCheck();
      },
      error: (error: any) => {
        console.error('Erro ao carregar eventos:', error);
      }
    });
  }

  loadProjects(): void {
    this.projectService.getAll().subscribe({
      next: (data: Project[]) => {
        this.projects = data;
        this.cdr.markForCheck();
      },
      error: (error: any) => {
        console.error('Erro ao carregar projetos:', error);
      }
    });
  }

  goBack(): void {
    this.router.navigate(['/dashboard']);
  }

  selectDate(date: Date): void {
    this.selectedDate = date;
    this.cdr.markForCheck();
  }

  getWeekDays(): string[] {
    const days = ['Seg', 'Ter', 'Qua', 'Qui', 'Sex', 'Sab', 'Dom'];
    return days;
  }

  getDaysInMonth(date: Date): number {
    return new Date(date.getFullYear(), date.getMonth() + 1, 0).getDate();
  }

  getFirstDayOfMonth(date: Date): number {
    return new Date(date.getFullYear(), date.getMonth(), 1).getDay();
  }

  getPreviousMonth(): void {
    this.currentDate = new Date(this.currentDate.getFullYear(), this.currentDate.getMonth() - 1);
    this.cdr.markForCheck();
  }

  getNextMonth(): void {
    this.currentDate = new Date(this.currentDate.getFullYear(), this.currentDate.getMonth() + 1);
    this.cdr.markForCheck();
  }

  getMonthName(): string {
    const months = ['Janeiro', 'Fevereiro', 'Março', 'Abril', 'Maio', 'Junho',
                    'Julho', 'Agosto', 'Setembro', 'Outubro', 'Novembro', 'Dezembro'];
    return months[this.currentDate.getMonth()];
  }

  isToday(day: number): boolean {
    const today = new Date();
    return day === today.getDate() &&
           this.currentDate.getMonth() === today.getMonth() &&
           this.currentDate.getFullYear() === today.getFullYear();
  }

  isSelected(day: number): boolean {
    return day === this.selectedDate.getDate() &&
           this.currentDate.getMonth() === this.selectedDate.getMonth() &&
           this.currentDate.getFullYear() === this.selectedDate.getFullYear();
  }

  getCalendarDays(): (number | null)[] {
    const daysInMonth = this.getDaysInMonth(this.currentDate);
    const firstDay = this.getFirstDayOfMonth(this.currentDate);
    const days: (number | null)[] = [];

    // Adicionar dias em branco antes do primeiro dia do mês
    for (let i = 0; i < firstDay; i++) {
      days.push(null);
    }

    // Adicionar dias do mês
    for (let i = 1; i <= daysInMonth; i++) {
      days.push(i);
    }

    return days;
  }

  createDate(day: number): Date {
    return new Date(this.currentDate.getFullYear(), this.currentDate.getMonth(), day);
  }

  getEventsForDate(day: number): Event[] {
    const date = this.createDate(day);
    return this.events
      .filter(e => {
        const eventDate = new Date(e.date);
        return eventDate.getDate() === date.getDate() &&
               eventDate.getMonth() === date.getMonth() &&
               eventDate.getFullYear() === date.getFullYear();
      })
      .sort((a, b) => a.startTime.localeCompare(b.startTime));
  }

  openEventModal(): void {
    this.isEditingEvent = false;
    this.editingEventId = null;
    this.eventForm = {
      title: '',
      description: '',
      startTime: '09:00',
      endTime: '10:00',
      projectId: null,
      isApplicableToProject: false
    };
    this.showEventModal = true;

    // Recarregar projetos quando abre o modal para garantir dados frescos
    this.loadProjects();
    this.cdr.markForCheck();
  }

  closeEventModal(): void {
    this.showEventModal = false;
    this.cdr.markForCheck();
  }

  showAlert(title: string, message: string, type: string = 'info', confirmAction: () => void = () => {}): void {
    this.alertData = {
      title,
      message,
      type,
      confirmAction,
      confirmText: type === 'confirm' ? 'Confirmar' : 'OK',
      cancelText: 'Cancelar'
    };
    this.showAlertModal = true;
    this.cdr.markForCheck();
  }

  closeAlertModal(): void {
    this.showAlertModal = false;
    this.cdr.markForCheck();
  }

  confirmAlert(): void {
    this.alertData.confirmAction();
    this.closeAlertModal();
  }

  saveEvent(): void {
    if (!this.eventForm.title.trim()) {
      this.showAlert('Campo Obrigatório', 'Título é obrigatório');
      return;
    }

    if (!this.eventForm.startTime || !this.eventForm.endTime) {
      this.showAlert('Campos Obrigatórios', 'Hora de início e hora de fim são obrigatórias');
      return;
    }

    if (this.eventForm.endTime <= this.eventForm.startTime) {
      this.showAlert('Hora Inválida', 'A hora de fim não pode ser menor ou igual à hora de início');
      return;
    }

    const request: CreateEventRequest = {
      title: this.eventForm.title,
      description: this.eventForm.description,
      date: this.selectedDate,
      startTime: this.eventForm.startTime,
      endTime: this.eventForm.endTime,
      projectId: this.eventForm.projectId,
      isApplicableToProject: this.eventForm.isApplicableToProject
    };

    if (this.isEditingEvent && this.editingEventId) {
      this.eventService.updateEvent(this.editingEventId, request).subscribe({
        next: () => {
          this.loadEvents();
          this.closeEventModal();
        },
        error: (error: any) => {
          console.error('Erro ao atualizar evento:', error);
        }
      });
    } else {
      this.eventService.createEvent(request).subscribe({
        next: () => {
          this.loadEvents();
          this.closeEventModal();
        },
        error: (error: any) => {
          console.error('Erro ao criar evento:', error);
        }
      });
    }
  }

  deleteEvent(event: Event): void {
    this.showAlert(
      'Confirmar Eliminação',
      `Tem a certeza que deseja eliminar "${event.title}"?`,
      'confirm',
      () => {
        this.eventService.deleteEvent(event.id).subscribe({
          next: () => {
            this.loadEvents();
            this.showAlert('Sucesso', 'Evento eliminado com sucesso!', 'info');
          },
          error: (error: any) => {
            console.error('Erro ao eliminar evento:', error);
            this.showAlert('Erro', 'Erro ao eliminar evento. Por favor, tente novamente.');
          }
        });
      }
    );
  }
}
