import { Injectable } from '@angular/core';
import { CanActivate, Router } from '@angular/router';
import { KubernetesAuthService } from '../services/kubernetes-auth.service';

/**
 * Páginas reservadas ao Admin: gestão de utilizadores e registo global.
 *
 * O menu já não as mostra a quem não é Admin, mas o URL é escrevível à mão — sem isto, um
 * Leitor chegava à página e via um erro do servidor em vez de nunca lá entrar. Continua a ser
 * conveniência de navegação: quem garante o acesso é o `[RequerPapel]` da API.
 */
@Injectable({ providedIn: 'root' })
export class KubernetesAdminGuard implements CanActivate {
  constructor(private router: Router, private auth: KubernetesAuthService) {}

  canActivate(): boolean {
    if (!this.auth.isAuthenticated()) {
      this.router.navigate(['/login-kubernetes']);
      return false;
    }

    if (this.auth.isAdmin) {
      return true;
    }

    this.router.navigate(['/kubernetes']);
    return false;
  }
}
