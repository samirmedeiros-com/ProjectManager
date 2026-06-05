import { ProjectMember } from './project-member.model';
import { ProjectTask } from './task.model';

export interface Project {
  id: number;
  name: string;
  description?: string;
  startDate: Date;
  endDate?: Date;
  status: string;
  priority: number;
  manager: string;
  managerName?: string;
  ownerId?: number;
  ownerName?: string;
  setorId?: number;
  setorName?: string;
  freshDeskId?: string;
  commentCount?: number;
  completedAt?: Date;
  createdAt: Date;
  updatedAt: Date;
  members?: ProjectMember[];
  tasks?: ProjectTask[];
}

export interface CreateProjectRequest {
  name: string;
  description?: string;
  startDate: Date;
  endDate?: Date;
  priority?: number;
  manager: string;
  ownerId?: number | null;
  setorId?: number | null;
}

export interface UpdateProjectRequest {
  name?: string;
  description?: string;
  startDate?: Date;
  endDate?: Date;
  status?: string;
  priority?: number;
  ownerId?: number;
  setorId?: number;
}
