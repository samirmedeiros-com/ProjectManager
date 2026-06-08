import { Component, OnInit, ChangeDetectionStrategy, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { NavbarComponent } from '../navbar/navbar.component';

@Component({
  selector: 'app-agenda',
  standalone: true,
  imports: [CommonModule, NavbarComponent],
  templateUrl: './agenda.component.html',
  styleUrls: ['./agenda.component.css'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class AgendaComponent implements OnInit {
  currentDate: Date = new Date();
  selectedDate: Date = new Date();
  isLoading = false;

  constructor(
    private router: Router,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.loadAgendaData();
  }

  loadAgendaData(): void {
    this.isLoading = true;
    // TODO: Implementar carregamento de dados da agenda
    this.isLoading = false;
    this.cdr.markForCheck();
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
}
