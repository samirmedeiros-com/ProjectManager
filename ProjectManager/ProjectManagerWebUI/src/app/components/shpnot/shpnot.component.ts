import { CommonModule } from '@angular/common';
import { Component, OnInit, computed, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import {
  AbaShpNot,
  CampoShpNot,
  NoShpNot,
  ShpNotDetalhe,
  ShpNotEstatisticas,
  ShpNotResumo,
  ShpNotService,
} from '../../services/shpnot.service';

@Component({
  selector: 'app-shpnot',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './shpnot.component.html',
  styleUrls: ['./shpnot.component.scss'],
})
export class ShpNotComponent implements OnInit {
  // ------------------------------------------------------------- filtros
  /**
   * Vazia por omissão: a listagem abre nos últimos a entrar, que saem pelo índice do IDT.
   * Escolher um dia obriga a varrer a tabela — o ecrã avisa quando isso acontece.
   */
  data = '';
  mpsid = '';
  volume = '';
  estado = '';
  respserv = '';

  readonly tamanhosPagina = [10, 50, 100];

  // ------------------------------------------------------------- estado
  estatisticas = signal<ShpNotEstatisticas | null>(null);

  linhas = signal<ShpNotResumo[]>([]);
  haMais = signal(false);
  pagina = signal(1);
  tamanho = signal(10);

  detalhe = signal<ShpNotDetalhe | null>(null);
  abaAtiva = signal('envio');
  /** Volumes e outras coleções abrem um de cada vez, pela chave da linha. */
  linhaAberta = signal<string | null>(null);

  aCarregar = signal(false);
  aCarregarDetalhe = signal(false);
  erro = signal('');
  procurou = signal(false);

  constructor(private servico: ShpNotService) {}

  ngOnInit(): void {
    this.carregar();
  }

  // ------------------------------------------------------------ derivados

  abaCorrente = computed<AbaShpNot | null>(
    () => this.detalhe()?.abas.find((a) => a.chave === this.abaAtiva()) ?? null,
  );

  /**
   * O hint de um campo. Diz as três identidades do mesmo dado — como chegou no JSON, onde
   * está guardado, e com que nome viaja para o AS400. Quem consulta um SHPNOT está quase
   * sempre a comparar uma destas três vistas com outra.
   */
  hint(campo: CampoShpNot): string {
    const partes = [
      campo.json ? `JSON: ${campo.json}` : 'Campo de controlo (não vem no JSON)',
      `Tabela: ${campo.tabela}.${campo.coluna}`,
      campo.alias ? `View: ${campo.alias}` : 'Não segue na VW_SHPNOT_AS400',
    ];
    return partes.join('\n');
  }

  /**
   * As listas guardadas como texto JSON mostram-se separadas por vírgulas; um `["a","b"]` no
   * meio de campos normais lê-se como lixo. Se o texto não for JSON válido, fica como está.
   */
  valor(campo: CampoShpNot): string {
    if (campo.valor === null || campo.valor === '') return '—';
    if (!campo.lista) return campo.valor;

    try {
      const lista = JSON.parse(campo.valor);
      return Array.isArray(lista) ? (lista.join(', ') || '—') : campo.valor;
    } catch {
      return campo.valor;
    }
  }

  /** Os blocos vazios ficam no fim: existem, mas não têm nada para ler. */
  ordenados(blocos: NoShpNot[]): NoShpNot[] {
    return [...blocos].sort((a, b) => Number(a.vazio) - Number(b.vazio));
  }

  // ------------------------------------------------------------ pesquisa

  carregar(): void {
    this.servico.estatisticas().subscribe({
      next: (e) => this.estatisticas.set(e),
      error: () => this.estatisticas.set(null),
    });
    this.pagina.set(1);
    this.procurar();
  }

  procurar(): void {
    this.aCarregar.set(true);
    this.erro.set('');

    this.servico
      .procurar({
        // Uma pesquisa por MPS ID ou por volume não leva dia — e pedi-lo seria pior: quem
        // tem o número de um envio às mãos raramente sabe o dia em que ele entrou.
        data: this.mpsid.trim() || this.volume.trim() ? undefined : this.data || undefined,
        mpsid: this.mpsid,
        volume: this.volume,
        estado: this.estado,
        respserv: this.respserv,
        pagina: this.pagina(),
        tamanho: this.tamanho(),
      })
      .subscribe({
        next: (p) => {
          this.linhas.set(p.itens);
          this.haMais.set(p.haMais);
          this.aCarregar.set(false);
          this.procurou.set(true);
        },
        error: (e) => {
          this.erro.set(e?.error ?? 'Não foi possível procurar SHPNOTs.');
          this.linhas.set([]);
          this.haMais.set(false);
          this.aCarregar.set(false);
          this.procurou.set(true);
        },
      });
  }

  limpar(): void {
    this.mpsid = '';
    this.volume = '';
    this.estado = '';
    this.respserv = '';
    this.data = '';
    this.carregar();
  }

  irPara(p: number): void {
    if (p < 1 || (p > this.pagina() && !this.haMais())) return;
    this.pagina.set(p);
    this.procurar();
  }

  mudarTamanho(t: number): void {
    this.tamanho.set(Number(t));
    this.pagina.set(1);
    this.procurar();
  }

  // ------------------------------------------------------------ detalhe

  abrir(linha: ShpNotResumo): void {
    this.aCarregarDetalhe.set(true);
    this.erro.set('');
    this.linhaAberta.set(null);

    this.servico.obter(linha.idt).subscribe({
      next: (d) => {
        this.detalhe.set(d);
        this.abaAtiva.set(d.abas[0]?.chave ?? 'envio');
        this.aCarregarDetalhe.set(false);
      },
      error: (e) => {
        this.erro.set(e?.error ?? 'Não foi possível abrir o SHPNOT.');
        this.aCarregarDetalhe.set(false);
      },
    });
  }

  fechar(): void {
    this.detalhe.set(null);
  }

  alternarLinha(id: string): void {
    this.linhaAberta.set(this.linhaAberta() === id ? null : id);
  }

  private hojeIso(): string {
    const hoje = new Date();
    const mes = `${hoje.getMonth() + 1}`.padStart(2, '0');
    const dia = `${hoje.getDate()}`.padStart(2, '0');
    return `${hoje.getFullYear()}-${mes}-${dia}`;
  }
}
