import { Component, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { TimesheetService } from '../../services/timesheet.service';
import { ProjectService } from '../../services/project.service';
import { Timesheet, TimesheetEntry, DAYS_OF_WEEK } from '../../models/timesheet.model';
import { Project } from '../../models/project.model';

@Component({
  selector: 'app-timesheet',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule],
  templateUrl: './timesheet.component.html',
  styleUrls: ['./timesheet.component.css'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class TimesheetComponent implements OnInit {
  daysOfWeek = DAYS_OF_WEEK;
  projects: Project[] = [];
  timesheets: Timesheet[] = [];
  currentTimesheet: Timesheet | null = null;
  hoursForm!: FormGroup;
  isLoading = false;
  message = '';
  messageType: 'success' | 'error' | 'info' = 'info';
  selectedWeekStart: Date | null = null;

  constructor(
    private timesheetService: TimesheetService,
    private projectService: ProjectService,
    private fb: FormBuilder
  ) {
    this.hoursForm = this.fb.group({});
  }

  ngOnInit(): void {
    this.loadProjects();
    this.loadMyTimesheets();
  }

  loadProjects(): void {
    this.isLoading = true;
    this.projectService.getProjects().subscribe(
      (data: Project[]) => {
        this.projects = data;
        this.isLoading = false;
      },
      (error) => {
        this.message = 'Erro ao carregar projetos';
        this.messageType = 'error';
        this.isLoading = false;
      }
    );
  }

  loadMyTimesheets(): void {
    this.timesheetService.getMyTimesheets().subscribe(
      (data) => {
        this.timesheets = data;
      },
      (error) => {
        this.message = 'Erro ao carregar timesheets';
        this.messageType = 'error';
      }
    );
  }

  createNewTimesheet(projectId: number): void {
    if (!this.selectedWeekStart) {
      this.message = 'Selecione uma data de início de semana';
      this.messageType = 'error';
      return;
    }

    const userId = parseInt(localStorage.getItem('userId') || '0', 10);
    if (!userId) {
      this.message = 'Usuário não identificado';
      this.messageType = 'error';
      return;
    }

    this.isLoading = true;
    this.timesheetService.createTimesheet(projectId, userId, this.selectedWeekStart).subscribe(
      (timesheet) => {
        this.currentTimesheet = timesheet;
        this.initializeForm();
        this.message = 'Timesheet criado com sucesso';
        this.messageType = 'success';
        this.isLoading = false;
        this.loadMyTimesheets();
      },
      (error) => {
        this.message = error.error?.message || 'Erro ao criar timesheet';
        this.messageType = 'error';
        this.isLoading = false;
      }
    );
  }

  selectTimesheet(timesheetId: number): void {
    this.isLoading = true;
    this.timesheetService.getTimesheet(timesheetId).subscribe(
      (timesheet) => {
        this.currentTimesheet = timesheet;
        this.initializeForm();
        this.isLoading = false;
        this.message = '';
      },
      (error) => {
        this.message = 'Erro ao carregar timesheet';
        this.messageType = 'error';
        this.isLoading = false;
      }
    );
  }

  initializeForm(): void {
    const group: any = {};
    this.daysOfWeek.forEach(day => {
      const entry = this.currentTimesheet?.entries.find(e => e.dayOfWeek === day.value);
      group[`hours_${day.value}`] = [
        { value: entry?.workHours || 0, disabled: this.currentTimesheet?.status !== 'Draft' },
        [Validators.required, Validators.min(0), Validators.max(12)]
      ];
      group[`notes_${day.value}`] = [
        { value: entry?.notes || '', disabled: this.currentTimesheet?.status !== 'Draft' }
      ];
    });
    this.hoursForm = this.fb.group(group);
  }

  saveHours(): void {
    if (!this.currentTimesheet || this.hoursForm.invalid) {
      this.message = 'Formulário inválido';
      this.messageType = 'error';
      return;
    }

    const entries: TimesheetEntry[] = this.daysOfWeek.map(day => ({
      dayOfWeek: day.value,
      workHours: parseFloat(this.hoursForm.get(`hours_${day.value}`)?.value || 0),
      notes: this.hoursForm.get(`notes_${day.value}`)?.value || undefined
    }));

    this.isLoading = true;
    this.timesheetService.updateEntries(this.currentTimesheet.id, entries).subscribe(
      (updated) => {
        this.currentTimesheet = updated;
        this.message = 'Horas atualizadas com sucesso';
        this.messageType = 'success';
        this.isLoading = false;
      },
      (error) => {
        this.message = error.error?.message || 'Erro ao atualizar horas';
        this.messageType = 'error';
        this.isLoading = false;
      }
    );
  }

  submitTimesheet(): void {
    if (!this.currentTimesheet) return;

    this.isLoading = true;
    this.timesheetService.submitTimesheet(this.currentTimesheet.id).subscribe(
      (updated) => {
        this.currentTimesheet = updated;
        this.initializeForm();
        this.message = 'Timesheet enviado para aprovação';
        this.messageType = 'success';
        this.isLoading = false;
        this.loadMyTimesheets();
      },
      (error) => {
        this.message = error.error?.message || 'Erro ao enviar timesheet';
        this.messageType = 'error';
        this.isLoading = false;
      }
    );
  }

  getTotalHours(): number {
    if (!this.hoursForm) return 0;
    return this.daysOfWeek.reduce((sum, day) => {
      const value = parseFloat(this.hoursForm.get(`hours_${day.value}`)?.value || 0);
      return sum + (isNaN(value) ? 0 : value);
    }, 0);
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
}
