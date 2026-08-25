import { Component, HostBinding, OnDestroy, OnInit, ChangeDetectorRef } from '@angular/core';
import { DecimalPipe } from '@angular/common';

import { ActivatedRoute } from '@angular/router';
import { Subscription, of, timer } from 'rxjs';
import { catchError, switchMap } from 'rxjs/operators';
import { TvErro, TvFatia, TvProjetoLinha, TvResposta, TvService, TvTarefaLinha } from '../../services/tv.service';
import { CARDS, SECCOES, TvCard, TvComparativo, TvKpi, TvKpiNomeado, TvSeccao } from './tv-cards';

/**
 * Mural para TV: sem login, sem interação, redesenha-se sozinho.
 * A chave de acesso vem do URL (`/tv?k=...`) e é a única credencial que existe.
 */
@Component({
  selector: 'app-tv',
  standalone: true,
  imports: [DecimalPipe],
  templateUrl: './tv.component.html',
  styleUrl: './tv.component.scss'
})
export class TvComponent implements OnInit, OnDestroy {
  dados: TvResposta | null = null;
  erro: string | null = null;
  aCarregar = true;
  relogio = '';

  // Tema claro por pedido pontual (ex.: parede muito iluminada); o mural
  // continua escuro por omissão — ver a nota em :host no SCSS.
  @HostBinding('class.tema-claro') temaClaro = false;

  // Modo overlay: fundo transparente e sem o nome do mural, para sobrepor a
  // outra fonte (ex.: captura de janela/OBS num video wall). Os cartões
  // mantêm o seu próprio fundo opaco — só a base do ecrã fica transparente.
  @HostBinding('class.tema-transparente') fundoTransparente = false;

  private chave = '';
  private subscricoes = new Subscription();
  private ciclo?: Subscription;
  private cicloAtualMs = 0;

  constructor(
    private rota: ActivatedRoute,
    private tv: TvService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.chave = this.rota.snapshot.queryParamMap.get('k') ?? '';
    const tema = this.rota.snapshot.queryParamMap.get('tema');
    this.temaClaro = tema === 'claro';
    this.fundoTransparente = tema === 'transparente';

    // O :host não alcança o <body>: sem isto o branco da página por trás do
    // componente continuava opaco, e "transparente" não era transparente de
    // verdade para quem embute isto (OBS, video wall).
    if (this.fundoTransparente) {
      document.documentElement.style.background = 'transparent';
      document.body.style.background = 'transparent';
    }

    if (!this.chave) {
      this.erro = 'Falta a chave de acesso no endereço.';
      this.aCarregar = false;
      return;
    }

    // O relógio corre à parte da atualização dos dados: um ecrã parado num
    // minuto antigo lê-se como avaria, mesmo quando os números estão certos.
    this.subscricoes.add(
      timer(0, 1000).subscribe(() => {
        this.relogio = new Date().toLocaleTimeString('pt-PT', { hour: '2-digit', minute: '2-digit' });
        this.cdr.markForCheck();
      })
    );

    // Intervalo fixo à partida; o servidor manda o seu na primeira resposta e
    // a partir daí é esse que vale.
    this.iniciarCiclo(60_000);
  }

  ngOnDestroy(): void {
    this.subscricoes.unsubscribe();
  }

  private iniciarCiclo(intervaloMs: number): void {
    this.cicloAtualMs = intervaloMs;
    this.ciclo?.unsubscribe();

    // O erro é apanhado DENTRO do switchMap: se subisse até ao subscribe matava
    // o timer, e uma falha de rede de um segundo deixava o mural congelado para
    // sempre — a avaria mais provável num ecrã que ninguém vigia.
    this.ciclo = timer(0, intervaloMs)
      .pipe(
        switchMap(() =>
          this.tv.obterDashboard(this.chave).pipe(
            catchError((e) => {
              this.erro = e?.status === 401
                ? 'Chave de acesso inválida.'
                : 'Sem ligação ao servidor.';
              this.aCarregar = false;
              this.cdr.detectChanges();
              return of(null);
            })
          )
        )
      )
      .subscribe((d) => {
        if (!d) return;

        // Os últimos dados bons ficam no ecrã mesmo depois de uma falha; o
        // cabeçalho mostra a hora deles, por isso não passa por atual.
        this.dados = d;
        this.erro = null;
        this.aCarregar = false;
        this.cdr.detectChanges();

        const pedido = Math.max(10, d.refreshSegundos || 60) * 1000;
        if (pedido !== this.cicloAtualMs) this.iniciarCiclo(pedido);
      });

    this.subscricoes.add(this.ciclo);
  }

  /**
   * Todas as faixas que têm cards declarados. O mural mantém a sua forma mesmo
   * com fontes em baixo: um ecrã que muda de disposição a cada falha obriga a
   * reprocurar tudo de cada vez que se olha para ele.
   */
  get seccoesVisiveis(): TvSeccao[] {
    return SECCOES.filter((s) => this.cardsDe(s).length > 0);
  }

  /** Todos os cards da secção, respondesse a fonte ou não. */
  cardsDe(seccao: TvSeccao): TvCard[] {
    if (!this.dados) return [];
    return CARDS.filter((c) => c.seccao === seccao.id);
  }

  /**
   * A fonte deste card não respondeu neste ciclo.
   *
   * O card fica no ecrã a dizer que está sem ligação, em vez de desaparecer.
   * O que **não** pode acontecer é mostrar zeros: um zero falso num mural é pior
   * do que um card ausente, porque ninguém desconfia dele. Daí o template ter de
   * consultar isto **antes** de chamar `card.dados()` — sem a fonte, a função do
   * card rebentaria a ler propriedades de undefined.
   */
  semLigacao(card: TvCard): boolean {
    return !this.dados || this.dados.fontes[card.fonte] === undefined;
  }

  get fontesEmFalha(): string[] {
    return this.dados?.fontesEmFalha ?? [];
  }

  // --- leitura tipada dos dados de cada card (o template não faz casts) ---

  kpi(card: TvCard): TvKpi {
    return card.dados(this.dados!) as TvKpi;
  }

  kpis(card: TvCard): TvKpiNomeado[] {
    return card.dados(this.dados!) as TvKpiNomeado[];
  }

  fatias(card: TvCard): TvFatia[] {
    return card.dados(this.dados!) as TvFatia[];
  }

  projetos(card: TvCard): TvProjetoLinha[] {
    return card.dados(this.dados!) as TvProjetoLinha[];
  }

  tarefas(card: TvCard): TvTarefaLinha[] {
    return card.dados(this.dados!) as TvTarefaLinha[];
  }

  comparativo(card: TvCard): TvComparativo {
    return card.dados(this.dados!) as TvComparativo;
  }

  erros(card: TvCard): TvErro[] {
    return card.dados(this.dados!) as TvErro[];
  }

  hora(iso: string | null): string {
    if (!iso) return '—';
    return new Date(iso).toLocaleTimeString('pt-PT', { hour: '2-digit', minute: '2-digit' });
  }

  /** Altura da coluna em percentagem do maior valor da série. */
  alturaColuna(fatia: TvFatia, todas: TvFatia[]): number {
    const maximo = Math.max(...todas.map((f) => f.total), 1);
    return Math.max(2, Math.round((fatia.total / maximo) * 100));
  }

  vazio(card: TvCard): boolean {
    if (card.tipo === 'kpi') return false;
    if (card.tipo === 'kpis') return this.kpis(card).length === 0;
    if (card.tipo === 'comparativo') return this.comparativo(card).grupos.length === 0;
    return (card.dados(this.dados!) as unknown[]).length === 0;
  }

  /** Largura da barra em percentagem do maior valor da série. */
  larguraBarra(fatia: TvFatia, todas: TvFatia[]): number {
    const maximo = Math.max(...todas.map((f) => f.total), 1);
    return Math.round((fatia.total / maximo) * 100);
  }

  prazoLegivel(dias: number): string {
    if (dias < -1) return `há ${Math.abs(dias)} dias`;
    if (dias === -1) return 'ontem';
    if (dias === 0) return 'hoje';
    if (dias === 1) return 'amanhã';
    return `em ${dias} dias`;
  }

  get atualizadoEm(): string {
    if (!this.dados) return '';
    return new Date(this.dados.geradoEm).toLocaleTimeString('pt-PT', { hour: '2-digit', minute: '2-digit' });
  }
}
