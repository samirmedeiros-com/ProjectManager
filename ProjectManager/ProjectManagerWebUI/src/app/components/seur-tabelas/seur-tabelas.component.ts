import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { SeurTabelasService } from '../../services/seur-tabelas.service';
import {
  SeurCpostal, SaveCpostalDto,
  SeurDestino, SaveDestinoDto,
  SeurProduct, SaveProductDto,
  SeurService, SaveServiceDto,
  CwentNum, SaveCwentNumDto, CreateCwentNumDto
} from '../../models/seur-tabelas.model';

const PAGE_SIZE = 100;

type TabName = 'cpostal' | 'destinos' | 'products' | 'services' | 'cwent';

@Component({
  selector: 'app-seur-tabelas',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './seur-tabelas.component.html',
  styleUrls: ['./seur-tabelas.component.css']
})
export class SeurTabelasComponent implements OnInit {
  activeTab: TabName = 'cpostal';

  search = '';
  loading = false;
  erro = '';
  successMsg = '';

  // modal
  showModal = false;
  modalMode: 'create' | 'edit' = 'create';
  modalLoading = false;
  modalErro = '';

  // delete confirm
  showDeleteConfirm = false;
  deleteIdtPending: number | string | null = null;

  // CPOSTAL
  cpostais: SeurCpostal[] = [];
  cpostalTotal = 0; cpostalPage = 1; cpostalTotalPages = 1;
  editCpostal: SaveCpostalDto = { postcode: '' };
  editIdtCpostal: number | null = null;

  // DESTINOS
  destinos: SeurDestino[] = [];
  destinosTotal = 0; destinosPage = 1; destinosTotalPages = 1;
  editDestino: SaveDestinoDto = { destinoCode: '' };
  editIdtDestino: number | null = null;

  // PRODUCTS
  products: SeurProduct[] = [];
  editProduct: SaveProductDto = { productCode: '' };
  editIdtProduct: number | null = null;

  // SERVICES
  services: SeurService[] = [];
  editService: SaveServiceDto = { serviceCode: '' };
  editIdtService: number | null = null;

  // CWENT
  cwents: CwentNum[] = [];
  cwentTotal = 0; cwentPage = 1; cwentTotalPages = 1;
  editCwent: SaveCwentNumDto = { bicNumber: 0 };
  editAccountCwent: string | null = null;
  newCwentAccount = '';

  constructor(private svc: SeurTabelasService, private cdr: ChangeDetectorRef) {}

  ngOnInit() {
    this.carregar();
  }

  setTab(tab: TabName) {
    this.activeTab = tab;
    this.search = '';
    this.erro = '';
    this.successMsg = '';
    this.cpostalPage = 1;
    this.destinosPage = 1;
    this.cwentPage = 1;
    this.carregar();
    this.cdr.detectChanges();
  }

  carregar(page?: number) {
    this.loading = true;
    this.erro = '';
    this.successMsg = '';
    const s = this.search.trim() || undefined;

    switch (this.activeTab) {
      case 'cpostal':
        if (page !== undefined) this.cpostalPage = page;
        this.svc.getCpostais(s, this.cpostalPage, PAGE_SIZE).subscribe({
          next: r => { this.cpostais = r.items; this.cpostalTotal = r.total; this.cpostalTotalPages = r.totalPages; this.loading = false; this.cdr.detectChanges(); },
          error: e => { this.erro = 'Erro ' + (e?.status || ''); this.loading = false; this.cdr.detectChanges(); }
        });
        break;
      case 'destinos':
        if (page !== undefined) this.destinosPage = page;
        this.svc.getDestinos(s, this.destinosPage, PAGE_SIZE).subscribe({
          next: r => { this.destinos = r.items; this.destinosTotal = r.total; this.destinosTotalPages = r.totalPages; this.loading = false; this.cdr.detectChanges(); },
          error: e => { this.erro = 'Erro ' + (e?.status || ''); this.loading = false; this.cdr.detectChanges(); }
        });
        break;
      case 'products':
        this.svc.getProducts(s).subscribe({
          next: d => { this.products = d; this.loading = false; this.cdr.detectChanges(); },
          error: e => { this.erro = 'Erro ' + (e?.status || ''); this.loading = false; this.cdr.detectChanges(); }
        });
        break;
      case 'services':
        this.svc.getServices(s).subscribe({
          next: d => { this.services = d; this.loading = false; this.cdr.detectChanges(); },
          error: e => { this.erro = 'Erro ' + (e?.status || ''); this.loading = false; this.cdr.detectChanges(); }
        });
        break;
      case 'cwent':
        if (page !== undefined) this.cwentPage = page;
        this.svc.getCwents(s, this.cwentPage, PAGE_SIZE).subscribe({
          next: r => { this.cwents = r.items; this.cwentTotal = r.total; this.cwentTotalPages = r.totalPages; this.loading = false; this.cdr.detectChanges(); },
          error: e => { this.erro = 'Erro ' + (e?.status || ''); this.loading = false; this.cdr.detectChanges(); }
        });
        break;
    }
  }

  irParaPagina(page: number) {
    this.carregar(page);
    this.cdr.detectChanges();
  }

  get currentPage(): number {
    if (this.activeTab === 'cpostal') return this.cpostalPage;
    if (this.activeTab === 'destinos') return this.destinosPage;
    return this.cwentPage;
  }
  get totalPages(): number {
    if (this.activeTab === 'cpostal') return this.cpostalTotalPages;
    if (this.activeTab === 'destinos') return this.destinosTotalPages;
    return this.cwentTotalPages;
  }

  // ---- Open modal ----

  abrirCriar() {
    this.modalMode = 'create';
    this.modalErro = '';
    this.resetForms();
    this.showModal = true;
    this.cdr.detectChanges();
  }

  abrirEditar(item: any) {
    this.modalMode = 'edit';
    this.modalErro = '';
    this.resetForms();

    switch (this.activeTab) {
      case 'cpostal':
        const c = item as SeurCpostal;
        this.editIdtCpostal = c.idt;
        this.editCpostal = { postcode: c.postcode, population: c.population, country: c.country, destFranchise: c.destFranchise, plataform: c.plataform };
        break;
      case 'destinos':
        const d = item as SeurDestino;
        this.editIdtDestino = d.idt;
        this.editDestino = { destinoCode: d.destinoCode, plataformCode: d.plataformCode, productCode: d.productCode, serviceCode: d.serviceCode, destination: d.destination, loadLine: d.loadLine, transportLine: d.transportLine };
        break;
      case 'products':
        const p = item as SeurProduct;
        this.editIdtProduct = p.idt;
        this.editProduct = { productCode: p.productCode, product: p.product, shortName: p.shortName };
        break;
      case 'services':
        const sv = item as SeurService;
        this.editIdtService = sv.idt;
        this.editService = { serviceCode: sv.serviceCode, service: sv.service, shortName: sv.shortName };
        break;
      case 'cwent':
        const cw = item as CwentNum;
        this.editAccountCwent = cw.account;
        this.editCwent = { ...cw };
        break;
    }

    this.showModal = true;
    this.cdr.detectChanges();
  }

  fecharModal() {
    this.showModal = false;
    this.cdr.detectChanges();
  }

  // ---- Save ----

  guardar() {
    this.modalLoading = true;
    this.modalErro = '';

    switch (this.activeTab) {
      case 'cpostal':
        if (this.modalMode === 'create') {
          this.svc.createCpostal(this.editCpostal).subscribe({
            next: () => { this.fecharModal(); this.showSuccess('Criado com sucesso'); this.carregar(); },
            error: e => { this.modalErro = 'Erro ' + (e?.status || ''); this.modalLoading = false; this.cdr.detectChanges(); }
          });
        } else {
          this.svc.updateCpostal(this.editIdtCpostal!, this.editCpostal).subscribe({
            next: () => { this.fecharModal(); this.showSuccess('Guardado com sucesso'); this.carregar(); },
            error: e => { this.modalErro = 'Erro ' + (e?.status || ''); this.modalLoading = false; this.cdr.detectChanges(); }
          });
        }
        break;
      case 'destinos':
        if (this.modalMode === 'create') {
          this.svc.createDestino(this.editDestino).subscribe({
            next: () => { this.fecharModal(); this.showSuccess('Criado com sucesso'); this.carregar(); },
            error: e => { this.modalErro = 'Erro ' + (e?.status || ''); this.modalLoading = false; this.cdr.detectChanges(); }
          });
        } else {
          this.svc.updateDestino(this.editIdtDestino!, this.editDestino).subscribe({
            next: () => { this.fecharModal(); this.showSuccess('Guardado com sucesso'); this.carregar(); },
            error: e => { this.modalErro = 'Erro ' + (e?.status || ''); this.modalLoading = false; this.cdr.detectChanges(); }
          });
        }
        break;
      case 'products':
        if (this.modalMode === 'create') {
          this.svc.createProduct(this.editProduct).subscribe({
            next: () => { this.fecharModal(); this.showSuccess('Criado com sucesso'); this.carregar(); },
            error: e => { this.modalErro = 'Erro ' + (e?.status || ''); this.modalLoading = false; this.cdr.detectChanges(); }
          });
        } else {
          this.svc.updateProduct(this.editIdtProduct!, this.editProduct).subscribe({
            next: () => { this.fecharModal(); this.showSuccess('Guardado com sucesso'); this.carregar(); },
            error: e => { this.modalErro = 'Erro ' + (e?.status || ''); this.modalLoading = false; this.cdr.detectChanges(); }
          });
        }
        break;
      case 'services':
        if (this.modalMode === 'create') {
          this.svc.createService(this.editService).subscribe({
            next: () => { this.fecharModal(); this.showSuccess('Criado com sucesso'); this.carregar(); },
            error: e => { this.modalErro = 'Erro ' + (e?.status || ''); this.modalLoading = false; this.cdr.detectChanges(); }
          });
        } else {
          this.svc.updateService(this.editIdtService!, this.editService).subscribe({
            next: () => { this.fecharModal(); this.showSuccess('Guardado com sucesso'); this.carregar(); },
            error: e => { this.modalErro = 'Erro ' + (e?.status || ''); this.modalLoading = false; this.cdr.detectChanges(); }
          });
        }
        break;
      case 'cwent':
        if (this.modalMode === 'create') {
          const createDto: CreateCwentNumDto = { account: this.newCwentAccount, ...this.editCwent };
          this.svc.createCwent(createDto).subscribe({
            next: () => { this.fecharModal(); this.showSuccess('Criado com sucesso'); this.carregar(); },
            error: e => { this.modalErro = e?.error?.message || 'Erro ' + (e?.status || ''); this.modalLoading = false; this.cdr.detectChanges(); }
          });
        } else {
          this.svc.updateCwent(this.editAccountCwent!, this.editCwent).subscribe({
            next: () => { this.fecharModal(); this.showSuccess('Guardado com sucesso'); this.carregar(); },
            error: e => { this.modalErro = 'Erro ' + (e?.status || ''); this.modalLoading = false; this.cdr.detectChanges(); }
          });
        }
        break;
    }
  }

  // ---- Delete ----

  pedirDelete(idt: number | string) {
    this.deleteIdtPending = idt;
    this.showDeleteConfirm = true;
    this.cdr.detectChanges();
  }

  confirmarDelete() {
    if (this.deleteIdtPending === null) return;
    const idt = this.deleteIdtPending;
    this.showDeleteConfirm = false;
    this.deleteIdtPending = null;

    let obs$;
    switch (this.activeTab) {
      case 'cpostal': obs$ = this.svc.deleteCpostal(idt as number); break;
      case 'destinos': obs$ = this.svc.deleteDestino(idt as number); break;
      case 'products': obs$ = this.svc.deleteProduct(idt as number); break;
      case 'services': obs$ = this.svc.deleteService(idt as number); break;
      case 'cwent': obs$ = this.svc.deleteCwent(idt as string); break;
      default: obs$ = this.svc.deleteProduct(idt as number); break;
    }
    obs$.subscribe({
      next: () => { this.showSuccess('Eliminado com sucesso'); this.carregar(); },
      error: e => { this.erro = 'Erro ao eliminar: ' + (e?.status || ''); this.cdr.detectChanges(); }
    });
  }

  cancelarDelete() {
    this.showDeleteConfirm = false;
    this.deleteIdtPending = null;
    this.cdr.detectChanges();
  }

  private showSuccess(msg: string) {
    this.successMsg = msg;
    this.modalLoading = false;
    this.cdr.detectChanges();
    setTimeout(() => { this.successMsg = ''; this.cdr.detectChanges(); }, 3000);
  }

  private resetForms() {
    this.editIdtCpostal = null;
    this.editCpostal = { postcode: '' };
    this.editIdtDestino = null;
    this.editDestino = { destinoCode: '' };
    this.editIdtProduct = null;
    this.editProduct = { productCode: '' };
    this.editIdtService = null;
    this.editService = { serviceCode: '' };
    this.editAccountCwent = null;
    this.newCwentAccount = '';
    this.editCwent = { bicNumber: 0 };
    this.modalLoading = false;
  }

  fmtDate(d: string): string {
    if (!d) return '';
    return new Date(d).toLocaleDateString('pt-PT');
  }
}
