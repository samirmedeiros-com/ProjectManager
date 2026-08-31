import { Injectable } from '@angular/core';
import { CanActivate, Router } from '@angular/router';
import { KubernetesAuthService } from '../services/kubernetes-auth.service';

/**
 * A Gestão Kubernetes tem login próprio: não basta haver sessão do Project Manager.
 * Isto é conveniência de navegação — quem garante o acesso é o [RequerApp] da API.
 */
@Injectable({ providedIn: 'root' })
export class KubernetesAuthGuard implements CanActivate {
  constructor(private router: Router, private auth: KubernetesAuthService) {}

  canActivate(): boolean {
    if (this.auth.isAuthenticated()) {
      return true;
    }

    this.router.navigate(['/login-kubernetes']);
    return false;
  }
}
