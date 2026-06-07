export interface TimesheetEntry {
  id?: number;
  projectId: number;
  projectName?: string;
  workHours: number;
  notes?: string;
}

export interface Timesheet {
  id: number;
  userId: number;
  userName: string;
  date: Date;
  status: 'Draft' | 'Submitted' | 'Approved' | 'Rejected';
  entries: TimesheetEntry[];
  totalHours: number;
  approvedAt?: Date;
  rejectionReason?: string;
}

export interface TimesheetListItem {
  id: number;
  date: Date;
  status: 'Draft' | 'Submitted' | 'Approved' | 'Rejected';
  totalHours: number;
  projectCount: number;
  approvedAt?: Date;
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
