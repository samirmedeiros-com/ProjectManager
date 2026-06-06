export interface TimesheetEntry {
  dayOfWeek: number;
  workHours: number;
  notes?: string;
}

export interface Timesheet {
  id: number;
  projectId: number;
  projectName?: string;
  userId: number;
  userName?: string;
  weekStartDate: Date;
  weekEndDate: Date;
  status: 'Draft' | 'Submitted' | 'Approved' | 'Rejected';
  entries: TimesheetEntry[];
  totalHours: number;
  approvedByName?: string;
  approvedAt?: Date;
  rejectionReason?: string;
  createdAt: Date;
}

export interface TimesheetListItem {
  id: number;
  projectName?: string;
  userName?: string;
  weekStartDate: Date;
  weekEndDate: Date;
  status: string;
  totalHours: number;
  createdAt: Date;
}

export const DAYS_OF_WEEK = [
  { value: 0, label: 'Domingo' },
  { value: 1, label: 'Segunda' },
  { value: 2, label: 'Terça' },
  { value: 3, label: 'Quarta' },
  { value: 4, label: 'Quinta' },
  { value: 5, label: 'Sexta' },
  { value: 6, label: 'Sábado' }
];
