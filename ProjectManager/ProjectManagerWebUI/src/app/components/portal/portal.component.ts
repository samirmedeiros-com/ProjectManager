import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-portal',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './portal.component.html',
  styleUrls: ['./portal.component.css']
})
export class PortalComponent implements OnInit {
  /** Preenchido quando um guard devolveu o utilizador ao portal por falta de permissão. */
  avisoAcesso = '';

  constructor(private router: Router, private route: ActivatedRoute) {}

  ngOnInit(): void {
    if (this.route.snapshot.queryParams['sessao'] === 'expirada') {
      this.avisoAcesso = 'A sua sessão expirou. Entre novamente para continuar.';
      return;
    }

    // Sem isto, um 403 leva o utilizador de volta ao portal sem explicação nenhuma,
    // e o cartão parece estar avariado.
    switch (this.route.snapshot.queryParams['opensearch']) {
      case 'semSetor':
        this.avisoAcesso = 'A Consulta OpenSearch está reservada aos utilizadores do setor IT.';
        break;
      case 'indisponivel':
        this.avisoAcesso =
          'Não foi possível abrir a Consulta OpenSearch: o serviço não respondeu. Tente novamente mais tarde.';
        break;
    }
  }

  navigateTo(path: string) {
    this.router.navigate([path]);
  }
}
