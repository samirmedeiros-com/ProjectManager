import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterLink, RouterLinkActive } from '@angular/router';
import { KubernetesAuthService } from '../../services/kubernetes-auth.service';

/**
 * Cabeçalho comum às três páginas da aplicação. Existe para o menu ser um só: replicá-lo em
 * cada página garantiria que mais cedo ou mais tarde ficariam diferentes.
 */
@Component({
  selector: 'app-kubernetes-menu',
  standalone: true,
  imports: [CommonModule, RouterLink, RouterLinkActive],
  templateUrl: './kubernetes-menu.component.html',
  styleUrls: ['./kubernetes-menu.component.css'],
})
export class KubernetesMenuComponent {
  constructor(private auth: KubernetesAuthService, private router: Router) {}

  get nome(): string {
    return this.auth.currentUserValue?.fullName ?? '';
  }

  get papel(): string {
    return this.auth.currentUserValue?.role ?? '';
  }

  get isAdmin(): boolean {
    return this.auth.isAdmin;
  }

  sair(): void {
    this.auth.logout();
    this.router.navigate(['/portal']);
  }
}
