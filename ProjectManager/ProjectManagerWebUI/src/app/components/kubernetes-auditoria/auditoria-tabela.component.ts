import { ChangeDetectorRef, Component, Input, OnChanges, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { KubernetesService, PaginaAuditoria, RegistoAuditoria } from '../../services/kubernetes.service';

/**
 * Tabela do registo de ações, paginada e sempre do mais recente para o mais antigo.
 * A mesma serve a página global e o popup de um deployment — o que muda são as entradas.
 */
@Component({
  selector: 'app-auditoria-tabela',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './auditoria-tabela.component.html',
  styleUrls: ['./auditoria-tabela.component.css'],
})
export class AuditoriaTabelaComponent implements OnInit, OnChanges {
  /** Fixos: no popup de um deployment não se navega para fora dele. */
  @Input() namespace?: string;
  @Input() deployment?: string;

  /** A página global mostra os filtros; o popup não precisa deles. */
  @Input() mostrarFiltros = false;

  @Input() tamanho = 25;

  registos: RegistoAuditoria[] = [];
  total = 0;
  pagina = 0;

  filtroAcao = '';
  filtroUtilizador = '';

  aCarregar = false;
  erro = '';

  /** Linhas com o "antes e depois" aberto. Fechado por omissão: o memo pode ser longo. */
  expandidos = new Set<number>();

  readonly acoes = [
    { valor: '', rotulo: 'Todas as ações' },
    { valor: 'login', rotulo: 'Entradas' },
    { valor: 'login-falhado', rotulo: 'Entradas recusadas' },
    { valor: 'parar', rotulo: 'Parar' },
    { valor: 'arrancar', rotulo: 'Arrancar' },
    { valor: 'reiniciar', rotulo: 'Reiniciar' },
    { valor: 'nota', rotulo: 'Informação alterada' },
  ];

  constructor(private k8s: KubernetesService, private cdr: ChangeDetectorRef) {}

  ngOnInit(): void {
    this.carregar();
  }

  ngOnChanges(): void {
    // Trocar de deployment sem voltar à primeira página mostraria a página 3 de outro serviço.
    this.pagina = 0;
    this.carregar();
  }

  carregar(): void {
    this.aCarregar = true;
    this.erro = '';

    this.k8s
      .auditoria({
        ns: this.namespace,
        deployment: this.deployment,
        acao: this.filtroAcao || undefined,
        utilizador: this.filtroUtilizador.trim() || undefined,
        pagina: this.pagina,
        tamanho: this.tamanho,
      })
      .subscribe({
        next: (p: PaginaAuditoria) => {
          this.registos = p.registos;
          this.expandidos.clear();
          this.total = p.total;
          this.pagina = p.pagina;
          this.aCarregar = false;
          this.cdr.detectChanges();
        },
        error: (err) => {
          this.aCarregar = false;
          this.erro =
            typeof err?.error === 'string' && err.error
              ? err.error
              : 'Não foi possível ler o registo de ações.';
          this.cdr.detectChanges();
        },
      });
  }

  alternarAlteracao(id: number): void {
    this.expandidos.has(id) ? this.expandidos.delete(id) : this.expandidos.add(id);
  }

  temAlteracao(r: RegistoAuditoria): boolean {
    return !!(r.valorAnterior || r.valorNovo);
  }

  aplicarFiltros(): void {
    this.pagina = 0;
    this.carregar();
  }

  get totalPaginas(): number {
    return Math.max(1, Math.ceil(this.total / this.tamanho));
  }

  get primeiroDaPagina(): number {
    return this.total === 0 ? 0 : this.pagina * this.tamanho + 1;
  }

  get ultimoDaPagina(): number {
    return Math.min(this.total, (this.pagina + 1) * this.tamanho);
  }

  anterior(): void {
    if (this.pagina === 0) return;
    this.pagina--;
    this.carregar();
  }

  seguinte(): void {
    if (this.pagina + 1 >= this.totalPaginas) return;
    this.pagina++;
    this.carregar();
  }

  rotuloAcao(acao: string): string {
    switch (acao) {
      case 'login': return 'Entrou';
      case 'login-falhado': return 'Entrada recusada';
      case 'parar': return 'Parou';
      case 'arrancar': return 'Arrancou';
      case 'reiniciar': return 'Reiniciou';
      case 'nota': return 'Alterou a informação';
      default: return acao;
    }
  }
}
