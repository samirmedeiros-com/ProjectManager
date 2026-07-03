export interface SeurUserDetail {
  id: number;
  email: string;
  fullName: string;
  role?: string;
  isActive: boolean;
  createdAt: string;
  lastLoginAt?: string;
}

export interface CreateSeurUserDto {
  email: string;
  fullName: string;
  role: string;
}

export interface CreateUserResponse {
  user: SeurUserDetail;
  tempPassword: string;
  emailSent: boolean;
}

export interface ResetPasswordResponse {
  tempPassword: string;
  emailSent: boolean;
}
