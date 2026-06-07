import { Component, OnInit, ChangeDetectionStrategy, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { NavbarComponent } from '../navbar/navbar.component';
import { TimesheetService } from '../../services/timesheet.service';
import { ProjectService } from '../../services/project.service';
import { Timesheet, TimesheetEntry, TimesheetListItem } from '../../models/timesheet.model';
import { Project } from '../../models/project.model';

@Component({
  selector: 'app-timesheet',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule, NavbarComponent],
  templateUrl: './timesheet.component.html',
  styleUrls: ['./timesheet.component.css'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class TimesheetComponent implements OnInit {
  projects: Project[] = [];
  timesheets: TimesheetListItem[] = [];
  currentTimesheet: Timesheet | null = null;

  selectedDate: string = new Date().toISOString().split('T')[0];
  selectedProjectId: number | null = null;
  selectedMonth: string = new Date().toISOString().split('T')[0].slice(0, 7);

  isLoading = false;
  message = '';
  messageType: 'success' | 'error' | 'info' = 'info';

  entryForm: FormGroup;

  constructor(
    private timesheetService: TimesheetService,
    private projectService: ProjectService,
    private fb: FormBuilder,
    private cdr: ChangeDetectorRef,
    private router: Router
  ) {
    this.entryForm = this.fb.group({
      workHours: ['', Validators.required],
      notes: ['']
    });
  }

  ngOnInit(): void {
    this.loadProjects();
    this.loadMyTimesheets();
    this.loadDayTimesheet();
  }

  loadProjects(): void {
    this.isLoading = true;
    this.projectService.getAll().subscribe(
      (data: Project[]) => {
        this.projects = data;
        this.isLoading = false;
        this.cdr.markForCheck();
      },
      (error: any) => {
        this.message = 'Erro ao carregar projetos';
        this.messageType = 'error';
        this.isLoading = false;
        this.cdr.markForCheck();
      }
    );
  }

  loadMyTimesheets(): void {
    this.timesheetService.getMyTimesheets().subscribe(
      (data: TimesheetListItem[]) => {
        this.timesheets = data;
        this.cdr.markForCheck();
      },
      (error: any) => {
        console.error('Erro ao carregar timesheets:', error);
      }
    );
  }

  loadDayTimesheet(): void {
    if (!this.selectedDate) return;

    const date = new Date(this.selectedDate);
    this.isLoading = true;

    this.timesheetService.getDayTimesheet(date).subscribe(
      (timesheet: Timesheet) => {
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
      (error: any) => {
        this.message = 'Erro ao carregar timesheet do dia';
        this.messageType = 'error';
        this.isLoading = false;
        this.cdr.markForCheck();
      }
    );
  }

  addProjectEntry(): void {
    if (!this.currentTimesheet) {
      this.message = 'Selecione um dia primeiro';
      this.messageType = 'error';
      this.cdr.markForCheck();
      return;
    }

    if (!this.selectedProjectId) {
      this.message = 'Selecione um projeto';
      this.messageType = 'error';
      this.cdr.markForCheck();
      return;
    }

    if (this.entryForm.invalid) {
      this.message = 'Preencha as horas';
      this.messageType = 'error';
      this.cdr.markForCheck();
      return;
    }

    const { workHours, notes } = this.entryForm.value;
    // Convert time format (hh:mm) to decimal hours
    const [hours, minutes] = workHours.split(':').map(Number);
    const decimalHours = hours + minutes / 60;

    this.isLoading = true;

    this.timesheetService.addProjectEntry(this.currentTimesheet.id, this.selectedProjectId, decimalHours, notes).subscribe(
      (updated: Timesheet) => {
        // Enrich entries with project names from local projects array
        updated.entries = updated.entries.map(entry => ({
          ...entry,
          projectName: entry.projectName || this.projects.find(p => p.id === entry.projectId)?.name || 'Projeto'
        }));
        this.currentTimesheet = updated;
        this.message = 'Projeto adicionado com sucesso';
        this.messageType = 'success';
        this.isLoading = false;
        this.entryForm.reset();
        this.selectedProjectId = null;
        this.loadMyTimesheets();
        this.cdr.markForCheck();
      },
      (error: any) => {
        this.message = error.error?.message || 'Erro ao adicionar projeto';
        this.messageType = 'error';
        this.isLoading = false;
        this.cdr.markForCheck();
      }
    );
  }

  removeProjectEntry(projectId: number): void {
    if (!this.currentTimesheet) return;

    this.isLoading = true;
    this.timesheetService.removeProjectEntry(this.currentTimesheet.id, projectId).subscribe(
      (updated: Timesheet) => {
        // Enrich entries with project names from local projects array
        updated.entries = updated.entries.map(entry => ({
          ...entry,
          projectName: entry.projectName || this.projects.find(p => p.id === entry.projectId)?.name || 'Projeto'
        }));
        this.currentTimesheet = updated;
        this.message = 'Projeto removido';
        this.messageType = 'success';
        this.isLoading = false;
        this.loadMyTimesheets();
        this.cdr.markForCheck();
      },
      (error: any) => {
        this.message = 'Erro ao remover projeto';
        this.messageType = 'error';
        this.isLoading = false;
        this.cdr.markForCheck();
      }
    );
  }

  submitTimesheet(): void {
    if (!this.currentTimesheet) return;

    this.isLoading = true;
    this.timesheetService.submitTimesheet(this.currentTimesheet.id).subscribe(
      (updated: Timesheet) => {
        // Enrich entries with project names from local projects array
        updated.entries = updated.entries.map(entry => ({
          ...entry,
          projectName: entry.projectName || this.projects.find(p => p.id === entry.projectId)?.name || 'Projeto'
        }));
        this.currentTimesheet = updated;
        this.message = 'Timesheet enviada para aprovação';
        this.messageType = 'success';
        this.isLoading = false;
        this.loadMyTimesheets();
        this.cdr.markForCheck();
      },
      (error: any) => {
        this.message = error.error?.message || 'Erro ao enviar timesheet';
        this.messageType = 'error';
        this.isLoading = false;
        this.cdr.markForCheck();
      }
    );
  }

  deleteTimesheet(): void {
    if (!this.currentTimesheet) return;

    if (!confirm('Tem a certeza que deseja apagar esta timesheet? Esta ação não pode ser desfeita.')) {
      return;
    }

    this.isLoading = true;
    this.timesheetService.deleteTimesheet(this.currentTimesheet.id).subscribe(
      () => {
        this.message = 'Timesheet apagada com sucesso';
        this.messageType = 'success';
        this.isLoading = false;
        this.currentTimesheet = null;
        this.loadMyTimesheets();
        this.cdr.markForCheck();
      },
      (error: any) => {
        this.message = error.error?.message || 'Erro ao apagar timesheet';
        this.messageType = 'error';
        this.isLoading = false;
        this.cdr.markForCheck();
      }
    );
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

  selectTimesheet(timesheetId: number): void {
    this.timesheetService.getTimesheet(timesheetId).subscribe(
      (timesheet: Timesheet) => {
        // Enrich entries with project names from local projects array
        timesheet.entries = timesheet.entries.map(entry => ({
          ...entry,
          projectName: entry.projectName || this.projects.find(p => p.id === entry.projectId)?.name || 'Projeto'
        }));
        this.currentTimesheet = timesheet;
        this.selectedDate = new Date(timesheet.date).toISOString().split('T')[0];
        this.message = '';
        this.cdr.markForCheck();
      },
      (error: any) => {
        this.message = 'Erro ao carregar timesheet';
        this.messageType = 'error';
        this.cdr.markForCheck();
      }
    );
  }

  onDateChange(): void {
    this.loadDayTimesheet();
  }

  goBack(): void {
    this.router.navigate(['/dashboard']);
  }

  getFilteredTimesheets(): TimesheetListItem[] {
    if (!this.selectedMonth) return this.timesheets;

    const [year, month] = this.selectedMonth.split('-');
    return this.timesheets.filter(ts => {
      const tsDate = new Date(ts.date);
      const tsYear = tsDate.getFullYear().toString();
      const tsMonth = String(tsDate.getMonth() + 1).padStart(2, '0');
      return tsYear === year && tsMonth === month;
    });
  }

  onMonthChange(): void {
    // Reset to first day of selected month
    const [year, month] = this.selectedMonth.split('-');
    this.selectedDate = `${year}-${month}-01`;
    this.loadDayTimesheet();
    this.cdr.markForCheck();
  }

  decimalToTimeFormat(decimalHours: number): string {
    const hours = Math.floor(decimalHours);
    const minutes = Math.round((decimalHours - hours) * 60);
    return `${String(hours).padStart(2, '0')}:${String(minutes).padStart(2, '0')}`;
  }
}
