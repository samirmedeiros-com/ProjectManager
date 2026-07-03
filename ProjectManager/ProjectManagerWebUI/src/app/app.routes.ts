import { Routes } from '@angular/router';
import { PortalComponent } from './components/portal/portal.component';
import { LoginComponent } from './components/login/login.component';
import { LoginSeurComponent } from './components/login-seur/login-seur.component';
import { DashboardComponent } from './components/dashboard/dashboard.component';
import { SeurDashboardComponent } from './components/seur-dashboard/seur-dashboard.component';
import { ProjectDetailComponent } from './components/project-detail/project-detail.component';
import { SetorListComponent } from './components/setor-list/setor-list.component';
import { UserListComponent } from './components/user-list/user-list.component';
import { DebugComponent } from './components/debug/debug.component';
import { AuthGuard } from './guards/auth.guard';
import { GestorGuard } from './guards/gestor.guard';
import { SeurAuthGuard } from './guards/seur-auth.guard';

export const routes: Routes = [
  { path: '', redirectTo: '/portal', pathMatch: 'full' },
  { path: 'portal', component: PortalComponent },

  // Project Manager
  { path: 'login', component: LoginComponent },
  { path: 'debug', component: DebugComponent },
  { path: 'dashboard', component: DashboardComponent, canActivate: [AuthGuard] },
  { path: 'projects/:id', component: ProjectDetailComponent, canActivate: [AuthGuard] },
  { path: 'setores', component: SetorListComponent, canActivate: [AuthGuard] },
  { path: 'users', component: UserListComponent, canActivate: [AuthGuard, GestorGuard] },

  // Gestão SEUR
  { path: 'login-seur', component: LoginSeurComponent },
  { path: 'seur/dashboard', component: SeurDashboardComponent, canActivate: [SeurAuthGuard] },

  { path: '**', redirectTo: '/portal' }
];
