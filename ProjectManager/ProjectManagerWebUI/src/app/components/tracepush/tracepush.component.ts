import { CommonModule } from '@angular/common';
import { Component, OnInit, computed, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import {
  EstadoPush,
  PushDetalhe,
  PushEstatisticas,
  PushPorHora,
  PushPorUserLogin,
  PushResumo,
  ReenvioLog,
  TracePushService,
} from '../../services/tracepush.service';

/** Uma barra do gráfico já resolvida em pixels — o template não faz contas. */
interface BarraHora {
  hora: number;
  etiqueta: string;
  total: number;
  enviados: number;
  erros: number;
  pendentes: number;
  alturaEnviados: number;
  alturaErros: number;
  alturaPendentes: number;
}

@Component({
  selector: 'app-tracepush',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './tracepush.component.html',
  styleUrls: ['./tracepush.component.scss'],
})
export class TracePushComponent implements OnInit {
  // ------------------------------------------------------------- filtros
  /** Por omissão o dia de hoje: é o que interessa em 99% das vezes que alguém abre isto. */
  data = this.hojeIso();

  /**
   * Vazio = nenhum cliente escolhido, TODOS = a lista inteira, qualquer outro valor = esse
   * cliente. Sem este terceiro estado a tabela abria sempre com as duas dezenas de clientes
   * do dia à frente de quem só queria ver um.
   */
  readonly TODOS = 'TODOS';
  userlogin = '';
  conta = '';
  guia = '';
  estado = '';

  readonly tamanhosPagina = [10, 50, 100];

  // ------------------------------------------------------------- estado
  /** Espelho reactivo de `userlogin`: o computed do report precisa de um signal para reagir. */
  escolhaCliente = signal('');

  estatisticas = signal<PushEstatisticas | null>(null);
  porUserLogin = signal<PushPorUserLogin[]>([]);
  porHora = signal<PushPorHora[]>([]);
  loginGrafico = signal('');

  linhas = signal<PushResumo[]>([]);
  total = signal(0);
  pagina = signal(1);
  tamanho = signal(10);

  selecionados = signal<Set<string>>(new Set());
  detalhe = signal<PushDetalhe | null>(null);
  aCarregarDetalhe = signal(false);
  separadorDetalhe = signal<'pedido' | 'resposta'>('resposta');

  logs = signal<ReenvioLog[]>([]);
  mostrarRegisto = signal(false);

  aCarregar = signal(false);
  aReenviar = signal(false);
  erro = signal('');
  aviso = signal('');
  /** Só depois da primeira pesquisa é que "sem resultados" quer dizer alguma coisa. */
  procurou = signal(false);

  constructor(private servico: TracePushService) {}

  ngOnInit(): void {
    this.carregarDia();
  }

  // ------------------------------------------------------------ derivados

  totalPaginas = computed(() => Math.max(1, Math.ceil(this.total() / this.tamanho())));

  haSelecao = computed(() => this.selecionados().size > 0);

  /** As linhas do report que estão à vista — ver o comentário em `userlogin`. */
  reportVisivel = computed<PushPorUserLogin[]>(() => {
    const escolha = this.escolhaCliente();
    if (!escolha) return [];
    if (escolha === this.TODOS) return this.porUserLogin();
    return this.porUserLogin().filter((r) => r.userLogin === escolha);
  });

  /** O dia mostrado é hoje? Muda o rótulo dos cartões — "Hoje" lê-se melhor que a data. */
  ehHoje = computed(() => this.estatisticas()?.dia?.slice(0, 10) === this.hojeIso());

  /**
   * As barras do gráfico. A escala é o maior total de qualquer hora e não o total do dia:
   * com a escala no total do dia, um dia normal desenhava 24 barras rasteiras e uma paragem
   * ficava indistinguível de tráfego baixo.
   */
  barras = computed<BarraHora[]>(() => {
    const dados = this.porHora();
    const maximo = Math.max(1, ...dados.map((h) => h.total));
    const altura = 150;

    return dados.map((h) => ({
      hora: h.hora,
      etiqueta: String(h.hora).padStart(2, '0'),
      total: h.total,
      enviados: h.enviados,
      erros: h.erros,
      pendentes: h.pendentes,
      alturaEnviados: (h.enviados / maximo) * altura,
      alturaErros: (h.erros / maximo) * altura,
      alturaPendentes: (h.pendentes / maximo) * altura,
    }));
  });

  // -------------------------------------------------------------- leituras

  /** Cartões + report por userlogin. Os dois olham para o mesmo dia, por isso andam juntos. */
  carregarDia(): void {
    this.erro.set('');

    this.servico.estatisticas(this.data).subscribe({
      next: (e) => this.estatisticas.set(e),
      // Os cartões são informativos: se falharem não estragam a pesquisa, ficam só sem números.
      error: () => this.estatisticas.set(null),
    });

    this.servico.porUserLogin(this.data).subscribe({
      next: (r) => this.porUserLogin.set(r),
      error: (e) => this.erro.set(this.mensagem(e)),
    });
  }

  procurar(reiniciarPagina = true): void {
    if (!this.guia.trim() && !this.data) {
      this.erro.set('Indique uma data, ou então um número de guia.');
      return;
    }

    if (reiniciarPagina) this.pagina.set(1);
    this.erro.set('');
    this.aCarregar.set(true);
    this.selecionados.set(new Set());

    this.servico
      .procurar({
        data: this.data,
        userlogin: this.clienteParaFiltro(),
        conta: this.conta,
        guia: this.guia,
        estado: this.estado,
        pagina: this.pagina(),
        tamanho: this.tamanho(),
      })
      .subscribe({
        next: (p) => {
          this.linhas.set(p.itens);
          this.total.set(p.total);
          this.procurou.set(true);
          this.aCarregar.set(false);
        },
        error: (e) => {
          this.erro.set(this.mensagem(e));
          this.aCarregar.set(false);
        },
      });
  }

  limparFiltros(): void {
    this.data = this.hojeIso();
    this.mudarCliente('');
    this.conta = '';
    this.guia = '';
    this.estado = '';
    this.linhas.set([]);
    this.total.set(0);
    this.procurou.set(false);
    this.loginGrafico.set('');
    this.porHora.set([]);
    this.carregarDia();
  }

  /** O select de cliente. TODOS não é um userlogin — é a ausência de filtro na pesquisa. */
  mudarCliente(valor: string): void {
    this.userlogin = valor;
    this.escolhaCliente.set(valor);
  }

  /** O filtro que vai para a API: TODOS e "nada escolhido" são ambos "sem filtro". */
  private clienteParaFiltro(): string {
    return this.userlogin && this.userlogin !== this.TODOS ? this.userlogin : '';
  }

  /** Clicar numa linha do report escolhe esse cliente e filtra a lista. */
  escolherUserLogin(login: string): void {
    this.mudarCliente(login);
    this.procurar();
  }

  /** Abre o gráfico em popup — ver `fecharGrafico` para o fecho. */
  verGrafico(login: string): void {
    if (!login) return;
    this.loginGrafico.set(login);
    this.porHora.set([]);
    this.servico.porHora(login, this.data).subscribe({
      next: (h) => this.porHora.set(h),
      error: (e) => this.erro.set(this.mensagem(e)),
    });
  }

  fecharGrafico(): void {
    this.loginGrafico.set('');
    this.porHora.set([]);
  }

  // ------------------------------------------------------------- paginação

  irPara(p: number): void {
    if (p < 1 || p > this.totalPaginas() || p === this.pagina()) return;
    this.pagina.set(p);
    this.procurar(false);
  }

  mudarTamanho(t: number): void {
    this.tamanho.set(Number(t));
    this.procurar();
  }

  // -------------------------------------------------------------- seleção

  alternar(hhpRowId: string, event: Event): void {
    // Parar a propagação: a linha inteira abre o detalhe, e marcar a caixa não é abrir.
    event.stopPropagation();
    const novo = new Set(this.selecionados());
    if (novo.has(hhpRowId)) novo.delete(hhpRowId);
    else novo.add(hhpRowId);
    this.selecionados.set(novo);
  }

  estaSelecionado(hhpRowId: string): boolean {
    return this.selecionados().has(hhpRowId);
  }

  todosSelecionados = computed(() => {
    const linhas = this.linhas();
    return linhas.length > 0 && linhas.every((l) => this.selecionados().has(l.hhpRowId));
  });

  alternarTodos(): void {
    if (this.todosSelecionados()) this.selecionados.set(new Set());
    else this.selecionados.set(new Set(this.linhas().map((l) => l.hhpRowId)));
  }

  // -------------------------------------------------------------- detalhe

  abrirDetalhe(linha: PushResumo): void {
    this.detalhe.set(null);
    this.aCarregarDetalhe.set(true);
    this.separadorDetalhe.set('resposta');

    this.servico.obter(linha.hhpRowId).subscribe({
      next: (d) => {
        this.detalhe.set(d);
        this.aCarregarDetalhe.set(false);
      },
      error: (e) => {
        this.erro.set(this.mensagem(e));
        this.aCarregarDetalhe.set(false);
      },
    });
  }

  fecharDetalhe(): void {
    this.detalhe.set(null);
    this.aCarregarDetalhe.set(false);
  }

  // -------------------------------------------------------------- reenvio

  reenviarSelecionados(): void {
    const ids = [...this.selecionados()];
    if (ids.length === 0) return;

    const confirmacao =
      ids.length === 1
        ? 'Repor este envio na fila?'
        : `Repor ${ids.length} envios na fila?`;
    if (!confirm(`${confirmacao}\n\nO envio é feito pelo processo automático de push, não daqui.`)) return;

    this.executarReenvio(ids);
  }

  reenviarDetalhe(): void {
    const d = this.detalhe();
    if (!d) return;
    if (!confirm('Repor este envio na fila?\n\nO envio é feito pelo processo automático de push, não daqui.')) return;
    this.executarReenvio([d.hhpRowId]);
    this.fecharDetalhe();
  }

  private executarReenvio(ids: string[]): void {
    this.aReenviar.set(true);
    this.erro.set('');
    this.aviso.set('');

    this.servico.reenviar(ids).subscribe({
      next: (r) => {
        this.aReenviar.set(false);
        this.selecionados.set(new Set());

        const emFalta = r.naoEncontrados.length;
        this.aviso.set(
          emFalta === 0
            ? `${r.repostos} ${r.repostos === 1 ? 'envio reposto' : 'envios repostos'} na fila.`
            : `${r.repostos} repostos; ${emFalta} não foram encontrados.`,
        );

        // Os números mudaram: o que estava em erro passou a pendente.
        this.carregarDia();
        this.procurar(false);
        if (this.loginGrafico()) this.verGrafico(this.loginGrafico());
      },
      error: (e) => {
        this.aReenviar.set(false);
        this.erro.set(this.mensagem(e));
      },
    });
  }

  // --------------------------------------------------------------- registo

  alternarRegisto(): void {
    const aAbrir = !this.mostrarRegisto();
    this.mostrarRegisto.set(aAbrir);
    if (!aAbrir) return;

    this.servico.logs(100, this.clienteParaFiltro() || undefined, this.guia || undefined).subscribe({
      next: (l) => this.logs.set(l),
      error: (e) => this.erro.set(this.mensagem(e)),
    });
  }

  // ---------------------------------------------------------------- ajudas

  etiquetaEstado(estado: EstadoPush): string {
    return { enviado: 'Enviado', erro: 'Erro', pendente: 'Pendente' }[estado] ?? estado;
  }

  private hojeIso(): string {
    // Data local e não toISOString(): em Portugal no verão o UTC já é do dia anterior à noite.
    const d = new Date();
    const mes = String(d.getMonth() + 1).padStart(2, '0');
    const dia = String(d.getDate()).padStart(2, '0');
    return `${d.getFullYear()}-${mes}-${dia}`;
  }

  private mensagem(e: unknown): string {
    const err = e as { error?: unknown; message?: string };
    if (typeof err?.error === 'string' && err.error.trim()) return err.error;
    return err?.message || 'Não foi possível completar a operação.';
  }
}
