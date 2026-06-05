export interface ProjectMember {
  id: number;
  projectId: number;
  userId: number;
  userName?: string;
  userEmail?: string;
  role: string;
  joinedAt: Date;
  isActive: boolean;
}

export interface AddProjectMemberRequest {
  userId: number;
  role?: string;
}
