export interface Setor {
  id: number;
  name: string;
  description?: string;
  isActive: boolean;
  createdAt: Date;
  updatedAt: Date;
}

export interface UserSetorDto {
  id: number;
  setorId: number;
  setorName: string;
  assignedAt: Date;
}

export interface AssignSetoresRequest {
  setorIds: number[];
}
