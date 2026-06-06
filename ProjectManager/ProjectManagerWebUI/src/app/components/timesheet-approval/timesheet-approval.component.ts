import { Component, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { TimesheetService } from '../../services/timesheet.service';
import { Timesheet, TimesheetListItem, DAYS_OF_WEEK } from '../../models/timesheet.model';

@Component({
  selector: 'app-timesheet-approval',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule],
  templateUrl: './timesheet-approval.component.html',
  styleUrls: ['./timesheet-approval.component.css'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class TimesheetApprovalComponent implements OnInit {
  daysOfWeek = DAYS_OF_WEEK;
  pendingTimesheets: TimesheetListItem[] = [];
  currentTimesheet: Timesheet | null = null;
  rejectForm!: FormGroup;
  showRejectModal = false;
  isLoading = false;
  message = '';
  messageType: 'success' | 'error' | 'info' = 'info';
  userSetorId: number = 0;

  constructor(
    private timesheetService: TimesheetService,
    private fb: FormBuilder
  ) {
    this.rejectForm = this.fb.group({
      reason: ['', [Validators.required, Validators.minLength(10)]]
    });
  }

  ngOnInit(): void {
    this.userSetorId = parseInt(localStorage.getItem('setorId') || '0', 10);
    this.loadPendingApprovals();
  }

  loadPendingApprovals(): void {
    this.isLoading = true;
    this.timesheetService.getPendingApprovals(this.userSetorId).subscribe(
      (data) => {
        this.pendingTimesheets = data;
        this.isLoading = false;
      },
      (error) => {
        this.message = 'Erro ao carregar timesheets pendentes';
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
      },
      (error) => {
        this.message = error.error?.message || 'Erro ao aprovar timesheet';
        this.messageType = 'error';
        this.isLoading = false;
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
      },
      (error) => {
        this.message = error.error?.message || 'Erro ao rejeitar timesheet';
        this.messageType = 'error';
        this.isLoading = false;
      }
    );
  }

  getTotalHours(): number {
    return this.currentTimesheet?.entries.reduce((sum, entry) => sum + entry.workHours, 0) || 0;
  }

  getDayLabel(dayOfWeek: number): string {
    const day = this.daysOfWeek.find(d => d.value === dayOfWeek);
    return day?.label || '';
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
