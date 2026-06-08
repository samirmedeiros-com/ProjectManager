export interface Event {
  id: number;
  title: string;
  description?: string;
  date: Date;
  startTime: string;
  endTime: string;
  projectId?: number;
  projectName?: string;
  isApplicableToProject: boolean;
  recurrenceType?: string;
  isRecurrenceParent?: boolean;
  parentEventId?: number;
}

export interface CreateEventRequest {
  title: string;
  description?: string;
  date: Date;
  startTime: string;
  endTime: string;
  projectId?: number | null;
  isApplicableToProject: boolean;
  recurrenceType?: string;
  recurrenceDaysOfWeek?: string;
  recurrenceEndDate?: Date;
  recurrenceEndCount?: number;
}

export interface UpdateEventRequest {
  title: string;
  description?: string;
  date: Date;
  startTime: string;
  endTime: string;
  projectId?: number | null;
  isApplicableToProject: boolean;
  recurrenceType?: string;
  recurrenceDaysOfWeek?: string;
  recurrenceEndDate?: Date;
  recurrenceEndCount?: number;
}
