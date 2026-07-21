import { CommonModule } from '@angular/common';
import { Component, OnInit, computed, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../services/auth.service';
import {
  CampoInfo,
  DocumentoResultado,
  EstadoCluster,
  IndiceInfo,
  OpenSearchService,
  ResultadoPesquisa,
} from '../../services/opensearch.service';

/** Quantas colunas mostrar por omissão num índice ainda desconhecido. */
const COLUNAS_INICIAIS = 6;

type Vista = 'pesquisa' | 'indices';

@Component({
  selector: 'app-opensearch',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './opensearch.component.html',
  styleUrls: ['./opensearch.component.scss'],
  host: {
    '(document:keydown.escape)': 'documentoAberto.set(null)',
  },
})
export class OpenSearchComponent implements OnInit {
  vista = signal<Vista>('pesquisa');

  estado = signal<EstadoCluster | null>(null);
  indices = signal<IndiceInfo[]>([]);
  campos = signal<CampoInfo[]>([]);

  indice = signal('');
  consulta = signal('');
  campoData = signal('');
  de = signal('');
  ate = signal('');
  ordenarPor = signal('');
  ordemDescendente = signal(true);
  tamanho = signal(25);
  pagina = signal(0);

  resultado = signal<ResultadoPesquisa | null>(null);
  colunasVisiveis = signal<string[]>([]);
  documentoAberto = signal<DocumentoResultado | null>(null);

  aCarregarIndices = signal(false);
  aPesquisar = signal(false);
  erro = signal<string | null>(null);
  /** Falha ao contactar o cluster de todo — distinta de um erro de pesquisa. */
  erroLigacao = signal<string | null>(null);

  camposTemporais = computed(() => this.campos().filter((c) => c.temporal));
  camposOrdenaveis = computed(() => this.campos().filter((c) => c.ordenavel));

  /**
   * Padrões sugeridos, derivados dos nomes reais. Os índices são diários
   * (`logs-app-<serviço>-2026.04.29`), e consultar um dia isolado raramente é o que se quer.
   * Gera todos os níveis do nome: "logs-*", "logs-app-*", "logs-app-<serviço>-*".
   */
  padroes = computed(() => {
    const contagem = new Map<string, { indices: number; documentos: number }>();

    for (const i of this.indices()) {
      const raiz = i.nome.replace(/[-.]\d{4}([-.]\d{2}){2}$/, '');
      if (raiz === i.nome) continue;

      const segmentos = raiz.split('-');
      for (let n = 1; n <= segmentos.length; n++) {
        const prefixo = segmentos.slice(0, n).join('-');
        const atual = contagem.get(prefixo) ?? { indices: 0, documentos: 0 };
        atual.indices++;
        atual.documentos += i.documentos;
        contagem.set(prefixo, atual);
      }
    }

    return [...contagem.entries()]
      .filter(([, v]) => v.indices > 1)
      .map(([prefixo, v]) => ({ padrao: `${prefixo}-*`, ...v }))
      .sort((a, b) => b.documentos - a.documentos);
  });

  ehPadrao = computed(() => /[*,]/.test(this.indice()));

  haQueLimpar = computed(
    () => !!(this.consulta() || this.de() || this.ate() || this.resultado() || this.erro()),
  );

  primeiroResultado = computed(() => this.pagina() * this.tamanho() + 1);

  ultimoResultado = computed(() => {
    const total = this.resultado()?.total ?? 0;
    return Math.min((this.pagina() + 1) * this.tamanho(), total);
  });

  haPaginaSeguinte = computed(() => {
    const total = this.resultado()?.total ?? 0;
    return (this.pagina() + 1) * this.tamanho() < total;
  });

  constructor(
    private api: OpenSearchService,
    private router: Router,
    private authService: AuthService,
  ) {}

  ngOnInit(): void {
    this.carregarEstado();
    this.carregarIndices();
  }

  get utilizador() {
    return this.authService.currentUserValue;
  }

  voltarAoPortal(): void {
    this.router.navigate(['/portal']);
  }

  // ── Carregamento ───────────────────────────────────────────────────

  carregarEstado(): void {
    this.api.estado().subscribe({
      next: (estado) => {
        this.estado.set(estado);
        this.erroLigacao.set(null);
      },
      error: (err) => this.erroLigacao.set(this.mensagemDe(err)),
    });
  }

  carregarIndices(): void {
    this.aCarregarIndices.set(true);
    this.api.indices().subscribe({
      next: (indices) => {
        this.indices.set(indices);
        this.aCarregarIndices.set(false);
        this.erroLigacao.set(null);
      },
      error: (err) => {
        this.aCarregarIndices.set(false);
        this.erroLigacao.set(this.mensagemDe(err));
      },
    });
  }

  /** Guarda o índice cujos campos já foram lidos, para não recarregar em cada blur do campo. */
  private ultimoCarregado = '';

  aoMudarIndice(nome: string): void {
    nome = nome.trim();
    if (nome === this.ultimoCarregado) return;
    this.ultimoCarregado = nome;
    this.indice.set(nome);

    this.campos.set([]);
    this.campoData.set('');
    this.ordenarPor.set('');
    this.resultado.set(null);
    this.colunasVisiveis.set([]);
    this.pagina.set(0);
    this.erro.set(null);

    if (!nome) return;

    this.api.campos(nome).subscribe({
      next: (campos) => {
        this.campos.set(campos);
        const temporal = campos.find((c) => c.temporal);
        if (temporal) {
          this.campoData.set(temporal.nome);
          this.ordenarPor.set(temporal.nome);
        }
      },
      error: (err) => this.erro.set(this.mensagemDe(err)),
    });
  }

  // ── Pesquisa ───────────────────────────────────────────────────────

  pesquisar(pagina = 0): void {
    if (!this.indice() || this.aPesquisar()) return;

    this.aPesquisar.set(true);
    this.erro.set(null);
    this.documentoAberto.set(null);
    this.pagina.set(pagina);

    this.api
      .pesquisar({
        indice: this.indice(),
        consulta: this.consulta().trim() || undefined,
        campoData: this.campoData() || undefined,
        de: this.paraIso(this.de()),
        ate: this.paraIso(this.ate()),
        ordenarPor: this.ordenarPor() || undefined,
        ordemDescendente: this.ordemDescendente(),
        pagina,
        tamanho: this.tamanho(),
      })
      .subscribe({
        next: (resultado) => {
          this.resultado.set(resultado);
          this.aPesquisar.set(false);

          const validas = this.colunasVisiveis().filter((c) => resultado.colunas.includes(c));
          this.colunasVisiveis.set(
            validas.length > 0 ? validas : resultado.colunas.slice(0, COLUNAS_INICIAIS),
          );
        },
        error: (err) => {
          this.aPesquisar.set(false);
          this.resultado.set(null);
          this.erro.set(this.mensagemDe(err));
        },
      });
  }

  /**
   * Limpa os critérios e os resultados, mas mantém o índice escolhido — a seguir a uma
   * pesquisa quase sempre quer-se outra no mesmo sítio, e recarregar o mapping é lento.
   */
  limpar(): void {
    this.consulta.set('');
    this.de.set('');
    this.ate.set('');
    this.resultado.set(null);
    this.colunasVisiveis.set([]);
    this.documentoAberto.set(null);
    this.pagina.set(0);
    this.erro.set(null);

    const temporal = this.camposTemporais()[0];
    this.ordenarPor.set(temporal ? temporal.nome : '');
    this.ordemDescendente.set(true);
  }

  paginaAnterior(): void {
    if (this.pagina() > 0) this.pesquisar(this.pagina() - 1);
  }

  paginaSeguinte(): void {
    if (this.haPaginaSeguinte()) this.pesquisar(this.pagina() + 1);
  }

  ordenarPorColuna(coluna: string): void {
    const campo = this.campos().find((c) => c.nome === coluna);
    // Ordenar por um campo "text" rebenta no cluster; o .keyword é que serve.
    if (campo && !campo.ordenavel) {
      const keyword = this.campos().find((c) => c.nome === `${coluna}.keyword`);
      if (!keyword) return;
      coluna = keyword.nome;
    }

    if (this.ordenarPor() === coluna) {
      this.ordemDescendente.update((v) => !v);
    } else {
      this.ordenarPor.set(coluna);
      this.ordemDescendente.set(true);
    }

    this.pesquisar(0);
  }

  consultarIndice(nome: string): void {
    this.vista.set('pesquisa');
    this.aoMudarIndice(nome);
    this.pesquisar(0);
  }

  alternarColuna(coluna: string): void {
    this.colunasVisiveis.update((atuais) =>
      atuais.includes(coluna)
        ? atuais.filter((c) => c !== coluna)
        : (this.resultado()?.colunas ?? []).filter((c) => atuais.includes(c) || c === coluna),
    );
  }

  // ── Correlação ─────────────────────────────────────────────────────

  /**
   * Campos que identificam um pedido/rastreio. São o atalho mais útil num portal de logs:
   * a partir de uma linha, ver tudo o que aconteceu no mesmo pedido.
   */
  camposCorrelacao(documento: DocumentoResultado): { chave: string; valor: string }[] {
    return Object.entries(documento.campos)
      .filter(([chave, valor]) => {
        const nome = chave.toLowerCase();
        const ehCorrelacao =
          nome.includes('requestid') ||
          nome.includes('traceid') ||
          nome.includes('spanid') ||
          nome.includes('correlation') ||
          nome.includes('traceidentifier');
        return ehCorrelacao && valor !== null && valor !== undefined && String(valor).length > 0;
      })
      .map(([chave, valor]) => ({ chave, valor: String(valor) }));
  }

  pesquisavel(valor: string): boolean {
    return valor !== '—' && valor.length > 0 && valor.length <= 1024;
  }

  pesquisarPorCampo(chave: string, valor: string): void {
    this.consulta.set(`${this.campoExato(chave)}:"${this.escaparLucene(valor)}"`);
    this.documentoAberto.set(null);
    this.pesquisar(0);
  }

  /**
   * Um campo "text" é analisado: pesquisar um GUID por ele parte-o nos hífenes e traz
   * documentos que só partilham um pedaço. O subcampo ".keyword" é que dá o termo exato.
   */
  private campoExato(chave: string): string {
    if (this.campos().some((c) => c.nome === `${chave}.keyword`)) return `${chave}.keyword`;
    return chave;
  }

  private escaparLucene(valor: string): string {
    return valor.replace(/\\/g, '\\\\').replace(/"/g, '\\"');
  }

  // ── Apresentação ───────────────────────────────────────────────────

  valor(documento: DocumentoResultado, coluna: string): string {
    return this.formatar(documento.campos[coluna]);
  }

  formatar(valor: unknown): string {
    if (valor === null || valor === undefined) return '—';
    if (typeof valor === 'object') return JSON.stringify(valor);
    return String(valor);
  }

  entradasDe(documento: DocumentoResultado): { chave: string; valor: string }[] {
    return Object.entries(documento.campos).map(([chave, valor]) => ({
      chave,
      valor: this.formatar(valor),
    }));
  }

  numero(valor: number | undefined | null): string {
    return (valor ?? 0).toLocaleString('pt-PT');
  }

  tamanhoLegivel(bytes: number | undefined | null): string {
    const valor = bytes ?? 0;
    if (valor < 1024) return `${valor} B`;

    const unidades = ['KB', 'MB', 'GB', 'TB', 'PB'];
    let n = valor / 1024;
    let i = 0;
    while (n >= 1024 && i < unidades.length - 1) {
      n /= 1024;
      i++;
    }
    return `${n.toFixed(n < 10 ? 1 : 0)} ${unidades[i]}`;
  }

  classeSaude(saude: string | undefined): string {
    switch (saude) {
      case 'green':
        return 'tag tag-green';
      case 'yellow':
        return 'tag tag-amber';
      case 'red':
        return 'tag tag-red';
      default:
        return 'tag tag-grey';
    }
  }

  saudeLegivel(saude: string | undefined): string {
    switch (saude) {
      case 'green':
        return 'Saudável';
      case 'yellow':
        return 'Degradado';
      case 'red':
        return 'Crítico';
      default:
        return 'Desconhecido';
    }
  }

  /** O input datetime-local dá hora local sem fuso; o cluster precisa de um instante absoluto. */
  private paraIso(valor: string): string | undefined {
    if (!valor) return undefined;
    const data = new Date(valor);
    return isNaN(data.getTime()) ? undefined : data.toISOString();
  }

  private mensagemDe(err: unknown): string {
    const erro = err as { error?: unknown; status?: number };

    if (typeof erro?.error === 'string' && erro.error.trim()) return erro.error;
    if (erro?.status === 403) return 'Acesso reservado aos utilizadores do setor IT.';
    if (erro?.status === 0) return 'Não foi possível contactar o servidor.';

    return 'Ocorreu uma falha inesperada ao consultar o OpenSearch.';
  }
}
