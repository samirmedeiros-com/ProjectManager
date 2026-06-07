import { Component, OnInit, ChangeDetectionStrategy, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { NavbarComponent } from '../navbar/navbar.component';
import { ReportService, HoursByProject, HoursByUser, HoursByMonth, ReportSummary } from '../../services/report.service';

@Component({
  selector: 'app-reports',
  standalone: true,
  imports: [CommonModule, FormsModule, NavbarComponent],
  templateUrl: './reports.component.html',
  styleUrls: ['./reports.component.css'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ReportsComponent implements OnInit {
  activeTab: 'summary' | 'projects' | 'users' | 'months' = 'summary';

  summary: ReportSummary | null = null;
  hoursByProject: HoursByProject[] = [];
  hoursByUser: HoursByUser[] = [];
  hoursByMonth: HoursByMonth[] = [];

  selectedDate: string = '';
  selectedMonthOffset: number = 0;
  isLoading = false;
  message = '';
  messageType: 'success' | 'error' | 'info' = 'info';

  constructor(
    private reportService: ReportService,
    private cdr: ChangeDetectorRef,
    private router: Router
  ) {}

  ngOnInit(): void {
    const today = new Date();
    this.selectedDate = today.toISOString().slice(0, 7);
    this.updateMonthOffset();
  }

  loadSummary(): void {
    this.isLoading = true;
    this.reportService.getSummary(this.selectedMonthOffset || undefined).subscribe(
      (data) => {
        this.summary = data;
        this.hoursByProject = data.hoursByProject;
        this.hoursByUser = data.hoursByUser;
        this.hoursByMonth = data.hoursByMonth;
        this.isLoading = false;
        this.cdr.markForCheck();
      },
      (error: any) => {
        this.message = 'Erro ao carregar relatório';
        this.messageType = 'error';
        this.isLoading = false;
        this.cdr.markForCheck();
      }
    );
  }

  onMonthOffsetChange(): void {
    this.updateMonthOffset();
  }

  updateMonthOffset(): void {
    if (!this.selectedDate) {
      this.selectedMonthOffset = 0;
      this.loadSummary();
      return;
    }

    const today = new Date();
    const currentYear = today.getFullYear();
    const currentMonth = today.getMonth();

    const [selectedYear, selectedMonth] = this.selectedDate.split('-').map(Number);

    // Calcular o monthOffset (diferença em meses, sempre positivo)
    let monthDiff = (currentYear - selectedYear) * 12 + (currentMonth - (selectedMonth - 1));
    this.selectedMonthOffset = Math.abs(monthDiff);

    this.loadSummary();
  }

  selectTab(tab: 'summary' | 'projects' | 'users' | 'months'): void {
    this.activeTab = tab;
    this.cdr.markForCheck();
  }

  getMonthName(month: number): string {
    const months = ['Jan', 'Fev', 'Mar', 'Abr', 'Mai', 'Jun', 'Jul', 'Ago', 'Set', 'Out', 'Nov', 'Dez'];
    return months[month - 1] || '';
  }

  getMonthLabel(): string {
    if (this.selectedMonthOffset === 0) {
      return 'Mês Atual';
    }
    const date = new Date();
    date.setMonth(date.getMonth() - this.selectedMonthOffset);
    return `${this.getMonthName(date.getMonth() + 1)} ${date.getFullYear()}`;
  }

  goBack(): void {
    this.router.navigate(['/dashboard']);
  }

  decimalToTimeFormat(decimalHours: number): string {
    const hours = Math.floor(decimalHours);
    const minutes = Math.round((decimalHours - hours) * 60);
    return `${String(hours).padStart(2, '0')}:${String(minutes).padStart(2, '0')}`;
  }
}
