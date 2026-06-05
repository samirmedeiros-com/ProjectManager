import { Setor } from './setor.model';

export interface User {
  id: number;
  email: string;
  fullName: string;
  department?: string;
  role?: string;
  isActive: boolean;
  createdAt: Date;
  lastLoginAt?: Date;
  setores?: Setor[];
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface LoginResponse {
  success: boolean;
  token?: string;
  message?: string;
  user?: User;
}

export interface RegisterRequest {
  fullName: string;
  email: string;
  password: string;
  role: string;
  department?: string;
  setorIds?: number[];
}

export interface UpdateUserRequest {
  fullName: string;
  email: string;
  role: string;
  isActive: boolean;
  department?: string;
  setorIds?: number[];
}

export interface RegisterResponse {
  success: boolean;
  message?: string;
  user?: User;
}
