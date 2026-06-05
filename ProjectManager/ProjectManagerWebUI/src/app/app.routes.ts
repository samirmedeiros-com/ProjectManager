import { Routes } from '@angular/router';
import { LoginComponent } from './components/login/login.component';
import { DashboardComponent } from './components/dashboard/dashboard.component';
import { ProjectDetailComponent } from './components/project-detail/project-detail.component';
import { SetorListComponent } from './components/setor-list/setor-list.component';
import { UserListComponent } from './components/user-list/user-list.component';
import { DebugComponent } from './components/debug/debug.component';
import { AuthGuard } from './guards/auth.guard';
import { GestorGuard } from './guards/gestor.guard';

export const routes: Routes = [
  { path: '', redirectTo: '/dashboard', pathMatch: 'full' },
  { path: 'login', component: LoginComponent },
  { path: 'debug', component: DebugComponent },
  { path: 'dashboard', component: DashboardComponent, canActivate: [AuthGuard] },
  { path: 'projects/:id', component: ProjectDetailComponent, canActivate: [AuthGuard] },
  { path: 'setores', component: SetorListComponent, canActivate: [AuthGuard] },
  { path: 'users', component: UserListComponent, canActivate: [AuthGuard, GestorGuard] },
  { path: '**', redirectTo: '/dashboard' }
];
