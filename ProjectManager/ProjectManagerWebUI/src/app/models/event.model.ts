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
}

export interface CreateEventRequest {
  title: string;
  description?: string;
  date: Date;
  startTime: string;
  endTime: string;
  projectId?: number;
  isApplicableToProject: boolean;
}

export interface UpdateEventRequest {
  title: string;
  description?: string;
  date: Date;
  startTime: string;
  endTime: string;
  projectId?: number;
  isApplicableToProject: boolean;
}
