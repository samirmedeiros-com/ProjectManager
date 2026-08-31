import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AuditoriaTabelaComponent } from './auditoria-tabela.component';
import { KubernetesMenuComponent } from '../kubernetes-shared/kubernetes-menu.component';

/** Vista global do registo de ações. Reservada ao Admin — a API recusa-a a quem não o seja. */
@Component({
  selector: 'app-kubernetes-auditoria',
  standalone: true,
  imports: [CommonModule, KubernetesMenuComponent, AuditoriaTabelaComponent],
  template: `
    <div class="pagina">
      <app-kubernetes-menu />
      <p class="intro">
        Todas as entradas na aplicação e todos os comandos executados sobre deployments,
        do mais recente para o mais antigo.
      </p>
      <app-auditoria-tabela [mostrarFiltros]="true" [tamanho]="25" />
    </div>
  `,
  styles: [`
    :host { display: block; min-height: 100vh; background: #f6f7f9; }
    .pagina { max-width: 1440px; margin: 0 auto; padding: 24px 28px 60px; }
    .intro { margin: 0 0 18px; color: #6b6d70; font-size: 13.5px; }
  `],
})
export class KubernetesAuditoriaComponent {}
