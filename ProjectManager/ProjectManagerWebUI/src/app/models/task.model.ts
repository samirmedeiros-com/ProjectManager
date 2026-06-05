export interface ProjectTask {
  id: number;
  projectId: number;
  title: string;
  description?: string;
  status: string;
  priority: string;
  dueDate: Date;
  assignedTo?: string;
  estimatedHours?: number;
  actualHours?: number;
  progress?: number;
  createdAt: Date;
  updatedAt: Date;
}

export interface CreateTaskRequest {
  title: string;
  description?: string;
  dueDate: Date;
  assignedTo?: string;
  estimatedHours?: number;
  priority?: string;
}

export interface UpdateTaskRequest {
  title?: string;
  description?: string;
  dueDate?: Date;
  status?: string;
  priority?: string;
  assignedTo?: string;
  actualHours?: number;
  progress?: number;
}
