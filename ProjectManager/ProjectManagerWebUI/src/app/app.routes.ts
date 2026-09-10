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
import { LoginOraConsoleComponent } from './components/login-oraconsole/login-oraconsole.component';
import { OraConsoleWorkbenchComponent } from './components/oraconsole-workbench/oraconsole-workbench.component';
import { OpenSearchComponent } from './components/opensearch/opensearch.component';
import { ContasComponent } from './components/contas/contas.component';
import { AppLoginComponent } from './components/app-login/app-login.component';
import { TvComponent } from './components/tv/tv.component';
import { LoginKubernetesComponent } from './components/login-kubernetes/login-kubernetes.component';
import { KubernetesComponent } from './components/kubernetes/kubernetes.component';
import { KubernetesAuditoriaComponent } from './components/kubernetes-auditoria/kubernetes-auditoria.component';
import { KubernetesUtilizadoresComponent } from './components/kubernetes-utilizadores/kubernetes-utilizadores.component';
import { AuthGuard } from './guards/auth.guard';
import { GestorGuard } from './guards/gestor.guard';
import { SeurAuthGuard } from './guards/seur-auth.guard';
import { OraConsoleAuthGuard } from './guards/oraconsole-auth.guard';
import { OpenSearchGuard } from './guards/opensearch.guard';
import { ContasGuard } from './guards/contas.guard';
import { KubernetesAuthGuard } from './guards/kubernetes-auth.guard';
import { KubernetesAdminGuard } from './guards/kubernetes-admin.guard';

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

  // OraConsole
  { path: 'login-oraconsole', component: LoginOraConsoleComponent },
  { path: 'oraconsole/workbench', component: OraConsoleWorkbenchComponent, canActivate: [OraConsoleAuthGuard] },

  // Consulta OpenSearch — credenciais da Gestão SEUR, com ecrã de login próprio.
  {
    path: 'login-opensearch',
    component: AppLoginComponent,
    data: {
      titulo: 'Consulta OpenSearch',
      subtitulo: 'Pesquisa nos logs das aplicações',
      cor: '#534ab7',
      returnUrlPadrao: '/opensearch',
      sigla: 'OS',
    },
  },
  { path: 'opensearch', component: OpenSearchComponent, canActivate: [OpenSearchGuard] },

  // Gestão de Dados — contas e subcontas; credenciais da Gestão SEUR, como o OpenSearch,
  // mas com ecrã de login próprio (partilha a base de utilizadores, muda a identidade).
  {
    path: 'login-contas',
    component: AppLoginComponent,
    data: {
      titulo: 'Gestão de Dados',
      subtitulo: 'Contas e subcontas · portal de clientes',
      cor: '#1d9e75',
      returnUrlPadrao: '/contas',
      sigla: 'GD',
    },
  },
  { path: 'contas', component: ContasComponent, canActivate: [ContasGuard] },

  // Gestão Kubernetes — login próprio, credenciais separadas das outras aplicações
  { path: 'login-kubernetes', component: LoginKubernetesComponent },
  { path: 'kubernetes', component: KubernetesComponent, canActivate: [KubernetesAuthGuard] },
  { path: 'kubernetes/registo', component: KubernetesAuditoriaComponent, canActivate: [KubernetesAuthGuard, KubernetesAdminGuard] },
  { path: 'kubernetes/utilizadores', component: KubernetesUtilizadoresComponent, canActivate: [KubernetesAuthGuard, KubernetesAdminGuard] },

  // Mural de TV — sem guard de propósito: não há sessão, o acesso é a chave em ?k=
  // que o backend valida em cada pedido. Sem chave o próprio componente avisa.
  { path: 'tv', component: TvComponent },

  { path: '**', redirectTo: '/portal' }
];
