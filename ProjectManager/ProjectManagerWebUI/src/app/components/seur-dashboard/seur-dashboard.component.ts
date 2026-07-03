import { Component, OnInit, ChangeDetectorRef, ViewChild } from '@angular/core';
import { Router } from '@angular/router';
import { CommonModule, DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { SeurAuthService, SeurUser } from '../../services/seur-auth.service';
import { SeurGuiaService } from '../../services/seur-guia.service';
import { SeurGuiaList, SeurGuiaDetail, SeurErro, SeurParcel, SeurVerify, SeurTotais } from '../../models/seur-guia.model';
import { SeurUsersComponent } from '../seur-users/seur-users.component';
import { SeurChangePasswordComponent } from '../seur-change-password/seur-change-password.component';
import { SeurTabelasComponent } from '../seur-tabelas/seur-tabelas.component';

type SeurView = 'home' | 'gestao' | 'usuarios' | 'tabelas';
type TabDados = 'geral' | 'origem' | 'destino' | 'estado';
type TabInfo  = 'request' | 'response' | 'erros' | 'ecb' | 'verify';
type Tab = TabDados | TabInfo;

@Component({
  selector: 'app-seur-dashboard',
  standalone: true,
  imports: [CommonModule, FormsModule, DatePipe, SeurUsersComponent, SeurChangePasswordComponent, SeurTabelasComponent],
  templateUrl: './seur-dashboard.component.html',
  styleUrls: ['./seur-dashboard.component.css']
})
export class SeurDashboardComponent implements OnInit {
  @ViewChild(SeurChangePasswordComponent) changePasswordModal!: SeurChangePasswordComponent;

  currentUser: SeurUser | null = null;
  get isAdmin(): boolean { return this.seurAuthService.isAdmin(); }

  // Vista
  seurView: SeurView = 'home';

  // Home dashboard
  dataDashboard = new Date().toISOString().substring(0, 10);
  totais: SeurTotais | null = null;
  loadingTotais = false;
  erroTotais = '';

  // Filtros
  filtroGuia = '';
  filtroReferencia = '';
  filtroData = new Date().toISOString().substring(0, 10);
  filtroFlagAtlas = '';
  usarFiltroGuia = false;
  usarFiltroReferencia = false;
  usarFiltroData = false;
  usarFiltroStatus = false;

  // Lista
  guias: SeurGuiaList[] = [];
  loadingLista = false;
  erroLista = '';

  // Detalhe
  guiaSelecionada: SeurGuiaDetail | null = null;
  loadingDetalhe = false;
  erroDetalhe = '';

  // Edição
  editMode = false;
  guiaEdit: SeurGuiaDetail | null = null;
  guardandoGuia = false;
  erroGuardar = '';

  // Tabs dados (editáveis)
  tabDados: TabDados = 'geral';
  // Tabs info (leitura)
  tabInfo: TabInfo = 'request';
  tabAtiva: Tab = 'geral'; // alias para compatibilidade

  erros: SeurErro[] = [];
  parcels: SeurParcel[] = [];
  verifies: SeurVerify[] = [];
  loadingTab = false;

  readonly flagAtlasOptions = [
    { value: 'N', label: 'Não Enviado' },
    { value: 'Y', label: 'Enviado' },
    { value: 'E', label: 'Erro' },
    { value: 'X', label: 'Outros' },
  ];
  readonly flagOptions = this.flagAtlasOptions;

  readonly verifyFlagOptions = [
    { value: 'N', label: 'Não Verificado' },
    { value: 'Y', label: 'Verificado' },
  ];

  readonly flagAs400Options = [
    { value: 'N', label: 'N' },
    { value: 'Y', label: 'Y' },
    { value: 'E', label: 'E' },
  ];

  readonly apagadoOptions = [
    { value: 'N', label: 'Não' },
    { value: 'Y', label: 'Sim' },
  ];

  readonly flagCitOptions = [
    { value: 'N', label: 'N' },
    { value: 'Y', label: 'Y' },
    { value: 'E', label: 'E' },
  ];

  constructor(
    private seurAuthService: SeurAuthService,
    private seurGuiaService: SeurGuiaService,
    private router: Router,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit() {
    this.currentUser = this.seurAuthService.currentUserValue;
    this.carregarTotais();
  }

  setView(v: SeurView) {
    this.seurView = v;
    if (v === 'home') this.carregarTotais();
    this.cdr.detectChanges();
  }

  carregarTotais() {
    this.loadingTotais = true;
    this.erroTotais = '';
    this.seurGuiaService.getTotais(this.dataDashboard).subscribe({
      next: (data) => { this.totais = data; this.loadingTotais = false; this.cdr.detectChanges(); },
      error: (err) => {
        this.erroTotais = 'Erro ' + (err?.status || err?.message || '');
        this.loadingTotais = false;
        this.cdr.detectChanges();
      }
    });
  }

  mudarDataDashboard(delta: number) {
    const d = new Date(this.dataDashboard + 'T12:00:00');
    d.setDate(d.getDate() + delta);
    this.dataDashboard = d.toISOString().substring(0, 10);
    this.carregarTotais();
  }

  pct(n: number): number {
    if (!this.totais?.total) return 0;
    return Math.round((n / this.totais.total) * 100);
  }

  irParaGestaoComFiltro(flagAtlas: string) {
    this.filtroFlagAtlas = flagAtlas;
    this.filtroData = this.dataDashboard;
    this.usarFiltroData = true;
    this.usarFiltroStatus = true;
    this.usarFiltroGuia = false;
    this.usarFiltroReferencia = false;
    this.seurView = 'gestao';
    this.cdr.detectChanges();
    this.pesquisar();
  }

  pesquisar() {
    this.loadingLista = true;
    this.erroLista = '';
    this.guiaSelecionada = null;
    this.guiaEdit = null;
    this.editMode = false;

    this.seurGuiaService.getGuias(
      this.usarFiltroGuia ? this.filtroGuia : undefined,
      this.usarFiltroReferencia ? this.filtroReferencia : undefined,
      this.usarFiltroData ? this.filtroData : undefined,
      this.usarFiltroStatus ? this.filtroFlagAtlas : undefined
    ).subscribe({
      next: (data) => {
        console.log('[SEUR] Guias recebidas:', data.length);
        this.guias = data;
        this.loadingLista = false;
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error('[SEUR] Erro ao carregar guias:', err);
        this.erroLista = 'Erro ao carregar guias: ' + (err?.message || err?.status || 'desconhecido');
        this.loadingLista = false;
        this.cdr.detectChanges();
      }
    });
  }

  limparFiltros() {
    this.filtroGuia = '';
    this.filtroReferencia = '';
    this.filtroData = new Date().toISOString().substring(0, 10);
    this.filtroFlagAtlas = '';
    this.usarFiltroGuia = false;
    this.usarFiltroReferencia = false;
    this.usarFiltroData = false;
    this.usarFiltroStatus = false;
    this.guias = [];
    this.guiaSelecionada = null;
    this.guiaEdit = null;
    this.editMode = false;
    this.erroLista = '';
  }

  selecionarGuia(guia: SeurGuiaList) {
    if (this.editMode) {
      if (!confirm('Tem alterações não guardadas. Deseja continuar?')) return;
    }
    this.loadingDetalhe = true;
    this.guiaSelecionada = null;
    this.guiaEdit = null;
    this.editMode = false;
    this.erroDetalhe = '';
    this.erros = [];
    this.parcels = [];
    this.verifies = [];
    this.tabDados = 'geral';
    this.tabInfo = 'request';

    this.seurGuiaService.getGuia(guia.idt).subscribe({
      next: (data) => {
        this.guiaSelecionada = data;
        this.loadingDetalhe = false;
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.erroDetalhe = 'Erro ' + (err?.status ?? err?.message ?? err);
        this.loadingDetalhe = false;
        this.cdr.detectChanges();
      }
    });
  }

  iniciarEdicao() {
    if (!this.guiaSelecionada) return;
    this.guiaEdit = { ...this.guiaSelecionada };
    this.editMode = true;
    this.erroGuardar = '';
    this.cdr.detectChanges();
  }

  cancelarEdicao() {
    this.guiaEdit = null;
    this.editMode = false;
    this.erroGuardar = '';
    this.cdr.detectChanges();
  }

  guardarGuia() {
    if (!this.guiaEdit) return;
    this.guardandoGuia = true;
    this.erroGuardar = '';

    this.seurGuiaService.updateGuia(this.guiaEdit.idt, this.guiaEdit).subscribe({
      next: () => {
        this.guiaSelecionada = { ...this.guiaEdit! };
        const item = this.guias.find(g => g.idt === this.guiaEdit!.idt);
        if (item) {
          item.guia = this.guiaEdit!.guia;
          item.referencia = this.guiaEdit!.referencia;
          item.destinoNome = this.guiaEdit!.destinoNome;
          item.destinoLocalidade = this.guiaEdit!.destinoLocalidade;
          item.destinoPais = this.guiaEdit!.destinoPais;
          item.flagAtlas = this.guiaEdit!.flagAtlas;
          item.flagAtlasDescricao = this.flagAtlasDescricao(this.guiaEdit!.flagAtlas);
          item.shipmentCode = this.guiaEdit!.shipmentCode;
        }
        this.editMode = false;
        this.guiaEdit = null;
        this.guardandoGuia = false;
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.erroGuardar = 'Erro ao guardar: ' + (err?.message || err?.status || 'desconhecido');
        this.guardandoGuia = false;
        this.cdr.detectChanges();
      }
    });
  }

  mudarTabDados(tab: TabDados) {
    this.tabDados = tab;
    this.cdr.detectChanges();
  }

  mudarTabInfo(tab: TabInfo) {
    if (!this.guiaSelecionada) return;
    this.tabInfo = tab;

    if (tab === 'erros' && this.erros.length === 0 && this.guiaSelecionada.referencia) {
      this.loadingTab = true;
      this.seurGuiaService.getErros(this.guiaSelecionada.idt, this.guiaSelecionada.referencia).subscribe({
        next: (data) => { this.erros = data; this.loadingTab = false; this.cdr.detectChanges(); },
        error: () => { this.loadingTab = false; this.cdr.detectChanges(); }
      });
    }
    if (tab === 'ecb' && this.parcels.length === 0 && this.guiaSelecionada.guia) {
      this.loadingTab = true;
      this.seurGuiaService.getParcels(this.guiaSelecionada.idt, this.guiaSelecionada.guia).subscribe({
        next: (data) => { this.parcels = data; this.loadingTab = false; this.cdr.detectChanges(); },
        error: () => { this.loadingTab = false; this.cdr.detectChanges(); }
      });
    }
    if (tab === 'verify' && this.verifies.length === 0 && this.guiaSelecionada.guia) {
      this.loadingTab = true;
      this.seurGuiaService.getVerify(this.guiaSelecionada.idt, this.guiaSelecionada.guia).subscribe({
        next: (data) => { this.verifies = data; this.loadingTab = false; this.cdr.detectChanges(); },
        error: () => { this.loadingTab = false; this.cdr.detectChanges(); }
      });
    }
    this.cdr.detectChanges();
  }

  // alias para não quebrar referências antigas no template
  mudarTab(tab: Tab) {
    if (tab === 'geral' || tab === 'origem' || tab === 'destino' || tab === 'estado') {
      this.mudarTabDados(tab);
    } else {
      this.mudarTabInfo(tab as TabInfo);
    }
  }

  guardarVerifyFlag(verify: SeurVerify) {
    this.seurGuiaService.updateVerifyFlag(verify.idt, verify.verifyFlag ?? 'N').subscribe({
      next: () => {
        verify.verifyFlagDescricao = this.verifyFlagOptions.find(f => f.value === verify.verifyFlag)?.label;
        this.cdr.detectChanges();
      }
    });
  }

  flagAtlasClass(flag?: string): string {
    switch (flag) {
      case 'Y': return 'badge badge-success';
      case 'E': return 'badge badge-danger';
      case 'N': return 'badge badge-warning';
      default:  return 'badge badge-secondary';
    }
  }

  flagAtlasDescricao(flag?: string): string {
    return this.flagAtlasOptions.find(f => f.value === flag)?.label ?? flag ?? '-';
  }

  logout() {
    this.seurAuthService.logout();
    this.router.navigate(['/portal']);
  }
}
