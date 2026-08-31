import { ChangeDetectorRef, Component, ElementRef, OnDestroy, OnInit, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { KubernetesAuthService } from '../../services/kubernetes-auth.service';
import { AuditoriaTabelaComponent } from '../kubernetes-auditoria/auditoria-tabela.component';
import { KubernetesMenuComponent } from '../kubernetes-shared/kubernetes-menu.component';
import {
  DeploymentInfo,
  KubernetesService,
  LinhaLog,
  NamespaceInfo,
  NotaDeployment,
  PodInfo,
} from '../../services/kubernetes.service';

type Comando = 'parar' | 'arrancar' | 'reiniciar';

interface PedidoConfirmacao {
  comando: Comando;
  deployment: DeploymentInfo;
  titulo: string;
  texto: string;
}

@Component({
  selector: 'app-kubernetes',
  standalone: true,
  imports: [CommonModule, FormsModule, KubernetesMenuComponent, AuditoriaTabelaComponent],
  templateUrl: './kubernetes.component.html',
  styleUrls: ['./kubernetes.component.css'],
})
export class KubernetesComponent implements OnInit, OnDestroy {
  namespaces: NamespaceInfo[] = [];
  namespaceAtivo = '';

  deployments: DeploymentInfo[] = [];
  filtro = '';

  aCarregar = false;
  erro = '';
  mensagem = '';

  /** Nome dos deployments com o painel de pods aberto (o "+" de cada linha). */
  expandidos = new Set<string>();
  pods: Record<string, PodInfo[]> = {};
  podsACarregar = new Set<string>();
  podsErro: Record<string, string> = {};

  /** Deployment com um comando a decorrer — desativa os botões dessa linha. */
  emComando = new Set<string>();

  confirmacao: PedidoConfirmacao | null = null;

  /** Deployment cujo histórico de ações está aberto no popup. */
  registoDe: DeploymentInfo | null = null;

  // ── Popup da informação ────────────────────────────────────────────
  notaDe: DeploymentInfo | null = null;
  nota = { titulo: '', memo: '' };
  notaOriginal: NotaDeployment | null = null;
  notaACarregar = false;
  notaAGravar = false;
  notaErro = '';

  readonly tituloMaximo = 100;

  // ── Popup do log ───────────────────────────────────────────────────
  podLog: PodInfo | null = null;
  contentorLog = '';
  linhasLog: LinhaLog[] = [];
  logACarregar = false;
  logErro = '';

  /**
   * Ligado, o log segue em tempo real e a caixa acompanha o fim. Desligado, a leitura pára:
   * o que está no ecrã fica congelado para se poder ler sem o texto fugir.
   */
  autoscroll = true;

  private ultimoLog: string | null = null;
  private temporizadorLog?: ReturnType<typeof setInterval>;

  /** O último bloco pedido ao servidor; enquanto não chegar, não se pede outro. */
  private aLerLog = false;

  @ViewChild('caixaLog') private caixaLog?: ElementRef<HTMLDivElement>;

  atualizacaoAutomatica = true;
  private temporizador?: ReturnType<typeof setInterval>;

  constructor(
    private k8s: KubernetesService,
    private auth: KubernetesAuthService,
    private cdr: ChangeDetectorRef,
  ) {}

  ngOnInit(): void {
    this.carregarNamespaces();
    this.ligarTemporizador();
  }

  ngOnDestroy(): void {
    this.desligarTemporizador();
    this.pararLeituraLog();
  }

  // ── Sessão ───────────────────────────────────────────────────────────

  get podeExecutarComandos(): boolean {
    return this.auth.podeExecutarComandos;
  }

  // ── Carregamento ─────────────────────────────────────────────────────

  private carregarNamespaces(): void {
    this.k8s.namespaces().subscribe({
      next: (lista) => {
        this.namespaces = lista;

        if (!this.namespaceAtivo && lista.length > 0) {
          this.namespaceAtivo = lista[0].nome;
          this.carregarDeployments();
        }

        this.cdr.detectChanges();
      },
      error: (err) => {
        this.erro = this.explicar(err, 'Não foi possível ler os namespaces do cluster.');
        this.cdr.detectChanges();
      },
    });
  }

  escolherNamespace(nome: string): void {
    if (nome === this.namespaceAtivo) return;

    this.namespaceAtivo = nome;

    // Os painéis abertos pertencem ao namespace anterior: mantê-los mostraria os pods
    // de um deployment que já não está na lista.
    this.expandidos.clear();
    this.pods = {};
    this.podsErro = {};

    this.carregarDeployments();
  }

  carregarDeployments(silencioso = false): void {
    if (!this.namespaceAtivo) return;

    if (!silencioso) {
      this.aCarregar = true;
      this.erro = '';
    }

    const namespacePedido = this.namespaceAtivo;

    this.k8s.deployments(namespacePedido).subscribe({
      next: (lista) => {
        // Uma resposta lenta de um namespace já abandonado não pode substituir a lista atual.
        if (namespacePedido !== this.namespaceAtivo) return;

        this.deployments = lista;
        this.aCarregar = false;
        this.erro = '';

        // Os painéis que ficaram abertos têm de acompanhar o estado novo.
        this.expandidos.forEach((nome) => this.carregarPods(nome, true));

        this.cdr.detectChanges();
      },
      error: (err) => {
        if (namespacePedido !== this.namespaceAtivo) return;

        this.aCarregar = false;
        this.erro = this.explicar(err, `Não foi possível listar os deployments de ${namespacePedido}.`);
        this.cdr.detectChanges();
      },
    });
  }

  get deploymentsFiltrados(): DeploymentInfo[] {
    const termo = this.filtro.trim().toLowerCase();
    if (!termo) return this.deployments;

    return this.deployments.filter(
      (d) => d.nome.toLowerCase().includes(termo) || d.imagem.toLowerCase().includes(termo),
    );
  }

  // ── Pods ─────────────────────────────────────────────────────────────

  alternarPods(deployment: DeploymentInfo): void {
    if (this.expandidos.has(deployment.nome)) {
      this.expandidos.delete(deployment.nome);
      return;
    }

    this.expandidos.add(deployment.nome);
    this.carregarPods(deployment.nome);
  }

  estaExpandido(nome: string): boolean {
    return this.expandidos.has(nome);
  }

  private carregarPods(nome: string, silencioso = false): void {
    if (!silencioso) {
      this.podsACarregar.add(nome);
      delete this.podsErro[nome];
    }

    this.k8s.pods(this.namespaceAtivo, nome).subscribe({
      next: (lista) => {
        this.pods[nome] = lista;
        this.podsACarregar.delete(nome);
        delete this.podsErro[nome];
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.podsACarregar.delete(nome);
        this.podsErro[nome] = this.explicar(err, 'Não foi possível ler os pods.');
        this.cdr.detectChanges();
      },
    });
  }

  // ── Informação escrita pela equipa ───────────────────────────────────

  abrirNota(d: DeploymentInfo): void {
    this.notaDe = d;
    this.nota = { titulo: '', memo: '' };
    this.notaOriginal = null;
    this.notaErro = '';
    this.notaACarregar = true;

    this.k8s.nota(this.namespaceAtivo, d.nome).subscribe({
      next: (n) => {
        this.notaACarregar = false;

        // Uma resposta lenta não pode escrever por cima de outro popup já aberto.
        if (this.notaDe !== d) return;

        this.notaOriginal = n;
        this.nota = { titulo: n.titulo ?? '', memo: n.memo ?? '' };
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.notaACarregar = false;
        if (this.notaDe !== d) return;

        this.notaErro = this.explicar(err, 'Não foi possível ler a informação deste deployment.');
        this.cdr.detectChanges();
      },
    });
  }

  fecharNota(): void {
    this.notaDe = null;
    this.notaOriginal = null;
    this.notaErro = '';
  }

  get caracteresRestantes(): number {
    return this.tituloMaximo - this.nota.titulo.length;
  }

  get notaAlterada(): boolean {
    return (
      this.nota.titulo.trim() !== (this.notaOriginal?.titulo ?? '') ||
      this.nota.memo.trim() !== (this.notaOriginal?.memo ?? '')
    );
  }

  gravarNota(): void {
    const alvo = this.notaDe;
    if (!alvo || this.notaAGravar) return;

    this.notaAGravar = true;
    this.notaErro = '';

    this.k8s
      .gravarNota(this.namespaceAtivo, alvo.nome, {
        titulo: this.nota.titulo.trim(),
        memo: this.nota.memo.trim(),
      })
      .subscribe({
        next: (n) => {
          this.notaAGravar = false;
          this.notaOriginal = n;

          // O título aparece por baixo do nome na lista: atualiza-se já, sem esperar
          // pela próxima leitura do cluster.
          const indice = this.deployments.findIndex((d) => d.nome === alvo.nome);
          if (indice >= 0) {
            this.deployments[indice] = { ...this.deployments[indice], titulo: n.titulo ?? null };
          }

          this.mensagem = `Informação de '${alvo.nome}' gravada.`;
          this.notaDe = null;
          this.cdr.detectChanges();
        },
        error: (err) => {
          this.notaAGravar = false;
          this.notaErro = this.explicar(err, 'Não foi possível gravar a informação.');
          this.cdr.detectChanges();
        },
      });
  }

  // ── Registo de ações de um deployment ────────────────────────────────

  abrirRegisto(d: DeploymentInfo): void {
    this.registoDe = d;
  }

  fecharRegisto(): void {
    this.registoDe = null;
  }

  // ── Log de um pod ────────────────────────────────────────────────────

  abrirLog(pod: PodInfo): void {
    this.podLog = pod;
    this.contentorLog = pod.contentores?.[0] ?? '';
    this.linhasLog = [];
    this.ultimoLog = null;
    this.logErro = '';
    this.autoscroll = true;
    this.logACarregar = true;

    this.lerLog();
    this.iniciarLeituraLog();
  }

  fecharLog(): void {
    this.pararLeituraLog();
    this.podLog = null;
    this.linhasLog = [];
    this.ultimoLog = null;
  }

  escolherContentor(nome: string): void {
    if (nome === this.contentorLog) return;

    // Outro contentor é outro log: recomeça-se do princípio, senão o `desde` do anterior
    // esconderia tudo o que este já tinha escrito.
    this.contentorLog = nome;
    this.linhasLog = [];
    this.ultimoLog = null;
    this.logErro = '';
    this.logACarregar = true;
    this.lerLog();
  }

  /**
   * É o mesmo interruptor para as duas coisas, como pedido: desligar pára a leitura e congela
   * o ecrã; voltar a ligar retoma o tempo real a partir da última linha recebida.
   */
  alternarAutoscroll(): void {
    this.autoscroll = !this.autoscroll;

    if (this.autoscroll) {
      this.lerLog();
      this.iniciarLeituraLog();
    } else {
      this.pararLeituraLog();
    }
  }

  private iniciarLeituraLog(): void {
    this.pararLeituraLog();
    this.temporizadorLog = setInterval(() => this.lerLog(), 2000);
  }

  private pararLeituraLog(): void {
    if (this.temporizadorLog) {
      clearInterval(this.temporizadorLog);
      this.temporizadorLog = undefined;
    }
  }

  private lerLog(): void {
    const pod = this.podLog;
    if (!pod || this.aLerLog) return;

    this.aLerLog = true;

    this.k8s
      .log(this.namespaceAtivo, pod.nome, {
        contentor: this.contentorLog || undefined,
        linhas: 500,
        desde: this.ultimoLog ?? undefined,
      })
      .subscribe({
        next: (resposta) => {
          this.aLerLog = false;
          this.logACarregar = false;
          this.logErro = '';

          // Uma resposta que chegue depois de fechar o popup (ou já de outro pod) não pode
          // escrever no ecrã atual.
          if (this.podLog !== pod) return;

          // O servidor devolve também as linhas do instante da fronteira, porque várias podem
          // partilhar o mesmo carimbo. As que já estão no ecrã descartam-se aqui.
          const novas = this.semRepetidas(resposta.linhas);

          if (novas.length > 0) {
            this.linhasLog = [...this.linhasLog, ...novas];

            // Teto de memória: um pod com muito débito encheria a página até a travar.
            if (this.linhasLog.length > 5000) {
              this.linhasLog = this.linhasLog.slice(-5000);
            }
          }

          if (resposta.ultimo) this.ultimoLog = resposta.ultimo;

          this.cdr.detectChanges();

          if (this.autoscroll) this.irParaOFim();
        },
        error: (err) => {
          this.aLerLog = false;
          this.logACarregar = false;

          if (this.podLog !== pod) return;

          this.logErro = this.explicar(err, 'Não foi possível ler o log deste pod.');

          // Um erro que se repete de 2 em 2 segundos encheria o ecrã de avisos: pára-se a
          // leitura e o utilizador retoma no interruptor.
          this.pararLeituraLog();
          this.autoscroll = false;
          this.cdr.detectChanges();
        },
      });
  }

  /** Descarta as linhas da fronteira que já estão no fim do que está a ser mostrado. */
  private semRepetidas(linhas: LinhaLog[]): LinhaLog[] {
    if (this.linhasLog.length === 0 || linhas.length === 0) return linhas;

    const fronteira = this.ultimoLog;
    if (!fronteira) return linhas;

    const jaVistas = new Set(
      this.linhasLog
        .filter((l) => l.tempo === fronteira)
        .map((l) => l.texto),
    );

    if (jaVistas.size === 0) return linhas;

    return linhas.filter((l) => !(l.tempo === fronteira && jaVistas.has(l.texto)));
  }

  private irParaOFim(): void {
    // Depois da deteção de alterações, senão o scrollHeight ainda é o de antes das linhas novas.
    setTimeout(() => {
      const caixa = this.caixaLog?.nativeElement;
      if (caixa) caixa.scrollTop = caixa.scrollHeight;
    });
  }

  // ── Comandos ─────────────────────────────────────────────────────────

  pedirParar(d: DeploymentInfo): void {
    this.confirmacao = {
      comando: 'parar',
      deployment: d,
      titulo: `Parar ${d.nome}?`,
      texto:
        `As ${d.replicasDesejadas} réplica(s) vão ser terminadas e o serviço deixa de responder. ` +
        'O número de réplicas fica guardado para o arranque o repor.',
    };
  }

  pedirArrancar(d: DeploymentInfo): void {
    const replicas = d.replicasAntesDeParar && d.replicasAntesDeParar > 0 ? d.replicasAntesDeParar : 1;

    this.confirmacao = {
      comando: 'arrancar',
      deployment: d,
      titulo: `Arrancar ${d.nome}?`,
      texto: `O deployment volta a ${replicas} réplica(s).`,
    };
  }

  pedirReiniciar(d: DeploymentInfo): void {
    this.confirmacao = {
      comando: 'reiniciar',
      deployment: d,
      titulo: `Reiniciar ${d.nome}?`,
      texto:
        'Os pods são substituídos um a um, sem parar o serviço — o mesmo que ' +
        'um rollout restart.',
    };
  }

  cancelarConfirmacao(): void {
    this.confirmacao = null;
  }

  confirmar(): void {
    const pedido = this.confirmacao;
    if (!pedido) return;

    this.confirmacao = null;

    const nome = pedido.deployment.nome;
    this.emComando.add(nome);
    this.erro = '';
    this.mensagem = '';

    const chamada =
      pedido.comando === 'parar' ? this.k8s.parar(this.namespaceAtivo, nome)
      : pedido.comando === 'arrancar' ? this.k8s.arrancar(this.namespaceAtivo, nome)
      : this.k8s.reiniciar(this.namespaceAtivo, nome);

    chamada.subscribe({
      next: (resultado) => {
        this.emComando.delete(nome);
        this.mensagem = resultado.mensagem;

        // A resposta traz o deployment já atualizado, mas o cluster leva segundos a
        // refletir as réplicas: substitui-se a linha agora e recarrega-se a seguir.
        const indice = this.deployments.findIndex((d) => d.nome === nome);
        if (indice >= 0) this.deployments[indice] = resultado.deployment;

        this.cdr.detectChanges();
        setTimeout(() => this.carregarDeployments(true), 2000);
      },
      error: (err) => {
        this.emComando.delete(nome);
        this.erro = this.explicar(err, `Não foi possível ${pedido.comando} '${nome}'.`);
        this.cdr.detectChanges();
      },
    });
  }

  estaEmComando(nome: string): boolean {
    return this.emComando.has(nome);
  }

  // ── Atualização automática ───────────────────────────────────────────

  alternarAtualizacao(): void {
    this.atualizacaoAutomatica = !this.atualizacaoAutomatica;
    this.atualizacaoAutomatica ? this.ligarTemporizador() : this.desligarTemporizador();
  }

  private ligarTemporizador(): void {
    this.desligarTemporizador();

    // Silenciosa de propósito: um spinner de 15 em 15 segundos faria a lista piscar
    // enquanto o utilizador a está a ler.
    this.temporizador = setInterval(() => {
      if (this.emComando.size === 0 && !this.confirmacao) {
        this.carregarDeployments(true);
      }
    }, 15000);
  }

  private desligarTemporizador(): void {
    if (this.temporizador) {
      clearInterval(this.temporizador);
      this.temporizador = undefined;
    }
  }

  // ── Apresentação ─────────────────────────────────────────────────────

  rotuloEstado(estado: string): string {
    switch (estado) {
      case 'pronto': return 'A correr';
      case 'parado': return 'Parado';
      case 'degradado': return 'Sem réplicas prontas';
      case 'a-atualizar': return 'A atualizar';
      default: return estado;
    }
  }

  /** Só a etiqueta da imagem: o caminho do registo ocupa a linha toda e não diz nada. */
  imagemCurta(imagem: string): string {
    const barra = imagem.lastIndexOf('/');
    return barra >= 0 ? imagem.slice(barra + 1) : imagem;
  }

  private explicar(err: unknown, alternativa: string): string {
    const erro = err as { error?: unknown; status?: number };

    if (typeof erro?.error === 'string' && erro.error.trim().length > 0) {
      return erro.error;
    }

    if (erro?.status === 403) {
      return 'Não tem permissão para executar esta operação.';
    }

    return alternativa;
  }
}
